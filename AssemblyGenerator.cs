using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace generate_to_assembly
{
    class AssemblyGenerator
    {
        const string SpecRunAssemblyName = "TechTalk.SpecRun.dll";

        public static void Generate(AssemblyGeneratorContext context, TextWriter errorOutput)
        {
            var referenceAssemblies = AssemblyReflecter.ProbeReferenceAssemblies(context.TemporaryPath);
            context.SourceAssembly = SpecFlowUtils.ProbeProjectAssemblyName(referenceAssemblies, context.TemporaryPath);
            var addedSpecRun = AddSpecRunIfNecessary(context, referenceAssemblies);

            var specFlowProject = SpecFlowUtils.ReadSpecFlowProject(context.TemporaryPath, context.SourceAssembly, context.DefaultNamespace);
            var generator = SpecFlowUtils.SetupGenerator(context.VerboseOutput, errorOutput);
            generator.ProcessProject(specFlowProject, false /* forceGeneration */);

            GenerateFeatureAssembly(
                context.TemporaryPath,
                referenceAssemblies.Select(assembly => assembly.FullPath).ToArray(),
                context.OutputPath,
                context.FeatureAssemblyName);

            CopyGeneratedAssemblies(context, addedSpecRun);
            GenerateConfiguration(
                Path.Combine(context.SourcePath, context.FeatureDllName + ".config"),
                AssemblyReflecter.GetStepAssemblyNames(referenceAssemblies, context.TemporaryPath));

            DeleteRedundantFiles(context.TemporaryPath);
        }

        static bool AddSpecRunIfNecessary(AssemblyGeneratorContext context, IList<LoadedAssembly> referenceAssemblies)
        {
            if (context.SourceAssembly.HasSpecFlowConfigured)
            {
                return false;
            }

            var specRunPath = Path.Combine(context.TemporaryPath, SpecRunAssemblyName);
            var addSpecRun = !File.Exists(specRunPath);
            if (addSpecRun)
            {
                File.Copy(Path.Combine(DirectoryUtils.GetContainingDirectory(Assembly.GetExecutingAssembly().Location), SpecRunAssemblyName), specRunPath, true);
                referenceAssemblies.Add(new LoadedAssembly { FullPath = specRunPath, Definition = null });
            }
            return addSpecRun;
        }

        static void GenerateFeatureAssembly(string featureCodeFilePath, string[] referenceAssemblies, string outputDirectory, string assemblyName)
        {
            var featureCodeFiles = Directory.GetFiles(featureCodeFilePath, "*.feature.cs", SearchOption.AllDirectories);
            if (featureCodeFiles.Length < 1)
            {
                throw new FileNotFoundException("No feature code file detected.", "*.feature.cs");
            }

            var parameters = new CompilerParameters()
            {
                GenerateInMemory = false,
                GenerateExecutable = false,
                IncludeDebugInformation = true,
                OutputAssembly = Path.Combine(outputDirectory, assemblyName + ".dll")
            };

            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.AddRange(referenceAssemblies);

            var provider = new CSharpCodeProvider();
            var results = provider.CompileAssemblyFromFile(parameters, featureCodeFiles);
            if (results.Errors.Count > 0)
            {
                var messages = new List<string>() { "Compliation error:" };
                foreach (CompilerError err in results.Errors)
                {
                    if (!err.IsWarning)
                    {
                        messages.Add(err.ToString());
                    }
                }

                messages.Add(string.Empty);
                messages.Add("Output from the compiler:");
                foreach (string output in results.Output)
                {
                    messages.Add(output);
                }

                throw new Exception(messages.JoinToString(Environment.NewLine));
            }
        }
        
        static void CopyGeneratedAssemblies(AssemblyGeneratorContext context, bool copySpecRun)
        {
            File.Copy(Path.Combine(context.OutputPath, context.FeatureDllName), Path.Combine(context.SourcePath, context.FeatureDllName), true);
            File.Copy(Path.Combine(context.OutputPath, context.FeatureAssemblyName + ".pdb"), Path.Combine(context.SourcePath, context.FeatureAssemblyName + ".pdb"), true);

            if (copySpecRun)
            {
                WriteSpecRunToSourcePath(context);
            }
        }

        private static void WriteSpecRunToSourcePath(AssemblyGeneratorContext context)
        {
            const string template = @"<?xml version=""1.0"" encoding=""utf-8""?>
<TestProfile xmlns=""http://www.specflow.org/schemas/plus/TestProfile/1.5"">
  <Settings projectName=""{0}"" projectId=""{{{1}}}"" />
  <Execution stopAfterFailures=""3"" testThreadCount=""1"" testSchedulingMode=""Sequential"" />
  <TestAssemblyPaths>
    <TestAssemblyPath>{2}</TestAssemblyPath>
  </TestAssemblyPaths>
  <DeploymentTransformation>
    <Steps>
    </Steps>
  </DeploymentTransformation>
</TestProfile>";

            File.Copy(Path.Combine(context.TemporaryPath, SpecRunAssemblyName), Path.Combine(context.SourcePath, SpecRunAssemblyName), true);

            var srProfilePath = Path.Combine(context.SourcePath, "Default.srprofile");
            if (!File.Exists(srProfilePath))
            {
                var srProfile = string.Format(template, context.SourceAssembly.AssemblyName, Guid.NewGuid().ToString(), context.FeatureDllName);
                File.WriteAllText(srProfilePath, srProfile, System.Text.Encoding.UTF8);
            }
        }

        static void GenerateConfiguration(string fileName, string[] assemblyNames)
        {
            const string configurationTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <configSections>
    <section name=""specFlow"" type=""TechTalk.SpecFlow.Configuration.ConfigurationSectionHandler, TechTalk.SpecFlow"" />
  </configSections>
  <specFlow>
    <stepAssemblies>
{0}
    </stepAssemblies>
  </specFlow>
</configuration>";
            const string assemblyTemplate = @"<stepAssembly assembly=""{0}"" />";

            var assembliesSection = assemblyNames
                                    .Select(n => string.Format(assemblyTemplate, n))
                                    .JoinToString(Environment.NewLine);
            var configuration = string.Format(configurationTemplate, assembliesSection);
            File.WriteAllText(fileName, configuration, System.Text.Encoding.UTF8);
        }
        
        static void DeleteRedundantFiles(string projectPath)
        {
            var reservedExtensions = new List<string>(new[] { ".feature", ".feature.cs" });
            Func<string, bool> shouldReserve = file => reservedExtensions.Any(extension => file.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase));

            Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .WhereNot(shouldReserve)
                .ToList()
                .ForEach(File.Delete);
        }

    }
}
