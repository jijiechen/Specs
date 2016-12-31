using System;
using System.IO;
using System.Linq;
using System.Xml;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.Configuration;
using TechTalk.SpecFlow.Generator.Interfaces;
using TechTalk.SpecFlow.Generator.Project;
using TechTalk.SpecFlow.Tracing;

namespace generate_to_assembly
{
    class SpecFlowUtils
    {
        
        public static SourceAssemblyProbe ProbeProjectAssemblyName(LoadedAssembly[] assemblies, string temporaryPath)
        {
            var configuredAssembly = assemblies.FirstOrDefault(HasSpecFlowConfigured);
            if (configuredAssembly != null)
            {
                return new SourceAssemblyProbe(configuredAssembly.FullPath, hasSpecFlowConfigured: true);
            }

            return new SourceAssemblyProbe(Path.Combine(temporaryPath, new DirectoryInfo(temporaryPath).Name + ".dll"), hasSpecFlowConfigured: false);
        }

        static bool HasSpecFlowConfigured(LoadedAssembly assembly)
        {
            var configuration = assembly.FullPath + ".config";
            if (!File.Exists(configuration))
            {
                return false;
            }


            try
            {
                var configurationDocument = new XmlDocument();
                configurationDocument.LoadXml(File.ReadAllText(configuration));

                return null != configurationDocument.SelectSingleNode("/configuration/specFlow");
            }
            catch
            {
                return false;
            }
        }

        public static SpecFlowProject ReadSpecFlowProject(string projectPath, SourceAssemblyProbe sourceAssemblyProbe, string defaultNameSpace = null)
        {
            var specFlowProject = new SpecFlowProject();
            var projectAssemblyName = sourceAssemblyProbe.AssemblyName;

            specFlowProject.ProjectSettings.ProjectFolder = projectPath;            
            specFlowProject.ProjectSettings.ProjectName = projectAssemblyName;

            var projectSettings = specFlowProject.ProjectSettings;
            projectSettings.AssemblyName = projectAssemblyName;
            projectSettings.DefaultNamespace = defaultNameSpace ?? projectAssemblyName;

            var featureFiles = Directory
                .GetFiles(projectPath, "*.feature", SearchOption.AllDirectories)
                .Select(file => new FeatureFileInput(file)
                {
                    CustomNamespace = projectSettings.DefaultNamespace +
                                          DirectoryUtils.GetContainingDirectory(file)
                                            .Replace(projectPath, string.Empty)
                                            .Replace(Path.DirectorySeparatorChar, '.')
                });
            specFlowProject.FeatureFiles.AddRange(featureFiles);

            Configure(specFlowProject, sourceAssemblyProbe);
            return specFlowProject;
        }

        static void Configure(SpecFlowProject specFlowProject, SourceAssemblyProbe sourceAssemblyProbe)
        {
            const string defaultConfiguration = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""specFlow"" type=""TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow"" />
  </configSections>
  <specFlow>
    <unitTestProvider name=""SpecRun"" />
    <plugins>    
      <add name=""SpecRun"" />
    </plugins>
</specFlow>
</configuration>";

            var configurationContent = sourceAssemblyProbe.HasSpecFlowConfigured
                                        ? File.ReadAllText(sourceAssemblyProbe.AssemblyPath + ".config")
                                        : defaultConfiguration;

            var configurationHolderFromFileContent = GetConfigurationHolderFromContent(configurationContent);
            specFlowProject.ProjectSettings.ConfigurationHolder = configurationHolderFromFileContent;
            specFlowProject.Configuration = new GeneratorConfigurationProvider().LoadConfiguration(configurationHolderFromFileContent);
        }

        static SpecFlowConfigurationHolder GetConfigurationHolderFromContent(string configurationContent)
        {
            try
            {
                var configurationDocument = new XmlDocument();
                configurationDocument.LoadXml(configurationContent);
                return new SpecFlowConfigurationHolder(configurationDocument.SelectSingleNode("/configuration/specFlow"));
            }
            catch (Exception)
            {
                return null;
            }
        }
        
        
        public static BatchGenerator SetupGenerator(bool verboseOutput, TextWriter errorOutput)
        {
            ITraceListener tracer;
            if (!verboseOutput)
            {
                ITraceListener traceListener = new NullListener();
                tracer = traceListener;
            }
            else
            {
                ITraceListener traceListener = new TextWriterTraceListener(Console.Out);
                tracer = traceListener;
            }

            var generator = new BatchGenerator(tracer, new TestGeneratorFactory());
            generator.OnError += OutputError(errorOutput);
            return generator;
        }

        static Action<FeatureFileInput, TestGeneratorResult> OutputError(TextWriter errorOutput)
        {
            return new Action<FeatureFileInput, TestGeneratorResult>((FeatureFileInput featureFileInput, TestGeneratorResult testGeneratorResult) =>
            {
                errorOutput.WriteLine("Error generating for file {0}", featureFileInput.ProjectRelativePath);
                errorOutput.WriteLine(
                        testGeneratorResult.Errors
                        .Select(e => string.Format("Line {0}:{1} - {2}", e.Line, e.LinePosition, e.Message))
                        .JoinToString(Environment.NewLine)
                    );
            });
        }

    }
}
