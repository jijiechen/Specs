﻿using Microsoft.CSharp;
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
        public static void Generate(AssemblyGeneratorContext context)
        {
            var specFlowProject = SpecFlowUtils.ReadSpecFlowProject(Path.Combine(context.TemporaryPath, context.SourceDllName), context.DefaultNamespace);
            var generator = SpecFlowUtils.SetupGenerator(context.VerboseOutput, Console.Error);
            generator.ProcessProject(specFlowProject, false /* forceGeneration */);


            GenerateFeatureAssembly(context.TemporaryPath, context.OutputPath, context.FeatureAssemblyName);
            CopyGeneratedAssembly(context);
            GenerateConfiguration(Path.Combine(context.SourcePath, context.FeatureDllName + ".config"), new[] { context.SourceAssemblyName });

            DeleteRedundantFiles(context.TemporaryPath);
        }
        
        static void GenerateFeatureAssembly(string referenceDirectory, string outputDirectory, string assemblyName)
        {
            var parameters = new CompilerParameters()
            {
                GenerateInMemory = false,
                GenerateExecutable = false,
                IncludeDebugInformation = true,
                OutputAssembly = Path.Combine(outputDirectory, assemblyName + ".dll")
            };

            var exes = Directory.GetFiles(referenceDirectory, "*.exe", SearchOption.TopDirectoryOnly);
            var dlls = Directory.GetFiles(referenceDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            var assemblies = exes.Concat(dlls).Where(IsAssembly).Where(f => !f.EndsWith(".features.dll")).ToArray();

            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.AddRange(assemblies);

            var featureCodeFiles = Directory.GetFiles(referenceDirectory, "*.feature.cs", SearchOption.AllDirectories);
            var provider = new CSharpCodeProvider();

            var results = provider.CompileAssemblyFromFile(parameters, featureCodeFiles);
            if (results.Errors.Count > 0)
            {
                var messages = new List<string>();
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

                throw new Exception("Compliation error:" + Environment.NewLine + String.Join(Environment.NewLine, messages));
            }
        }

        static bool IsAssembly(string assemblyPath)
        {
            try
            {
                AssemblyName.GetAssemblyName(assemblyPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void CopyGeneratedAssembly(AssemblyGeneratorContext context)
        {
            File.Copy(Path.Combine(context.OutputPath, context.FeatureDllName), Path.Combine(context.SourcePath, context.FeatureDllName), true);
            File.Copy(Path.Combine(context.OutputPath, context.FeatureAssemblyName + ".pdb"), Path.Combine(context.SourcePath, context.FeatureAssemblyName + ".pdb"), true);
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