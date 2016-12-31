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
        public static SpecFlowProject ReadSpecFlowProject(string assemblyFullPath, string defaultNameSpace = null)
        {
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyFullPath);
            var directoryName = Path.GetDirectoryName(assemblyFullPath);

            var specFlowProject = new SpecFlowProject();
            specFlowProject.ProjectSettings.ProjectFolder = directoryName;
            specFlowProject.ProjectSettings.ProjectName = assemblyName;

            var projectSettings = specFlowProject.ProjectSettings;
            projectSettings.AssemblyName = assemblyName;
            projectSettings.DefaultNamespace = defaultNameSpace ?? assemblyName;

            var featureFiles = Directory
                .GetFiles(directoryName, "*.feature", SearchOption.AllDirectories)
                .Select(file => new FeatureFileInput(file)
                {
                    CustomNamespace =
                    projectSettings.DefaultNamespace +
                    Path.GetDirectoryName(file)
                    .Replace(directoryName, string.Empty)
                    .Replace(Path.DirectorySeparatorChar, '.')
                });
            specFlowProject.FeatureFiles.AddRange(featureFiles);

            var configPath = Path.Combine(directoryName, Path.GetFileName(assemblyFullPath) + ".config");
            if (File.Exists(configPath))
            {
                var configurationHolderFromFileContent = GetConfigurationHolderFromFileContent(File.ReadAllText(configPath));
                specFlowProject.ProjectSettings.ConfigurationHolder = configurationHolderFromFileContent;
                specFlowProject.Configuration = new GeneratorConfigurationProvider().LoadConfiguration(configurationHolderFromFileContent);
            }
            return specFlowProject;
        }

        static SpecFlowConfigurationHolder GetConfigurationHolderFromFileContent(string configFileContent)
        {
            SpecFlowConfigurationHolder result;
            try
            {
                var configurationDocument = new XmlDocument();
                configurationDocument.LoadXml(configFileContent);
                result = new SpecFlowConfigurationHolder(configurationDocument.SelectSingleNode("/configuration/specFlow"));
            }
            catch (Exception)
            {
                result = new SpecFlowConfigurationHolder();
            }
            return result;
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
