using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using TechTalk.SpecFlow.Generator;
using TechTalk.SpecFlow.Generator.Configuration;
using TechTalk.SpecFlow.Generator.Interfaces;
using TechTalk.SpecFlow.Generator.Project;
using TechTalk.SpecFlow.Tracing;

namespace generate_to_assembly
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var defaultColor = Console.ForegroundColor;
            // System.Diagnostics.Debugger.Launch();
            const bool verboseOutput = true;

            var defaultNamespace = args.Get(1);
            var dll = args.Get(0);
            if(dll == null)
            {
                throw new ApplicationException("Please use first parameter to represent assembly path.");
            }
            dll = Path.IsPathRooted(dll) ? dll : Path.Combine(Environment.CurrentDirectory, dll);

            var sourceDirectory = Path.GetDirectoryName(dll);
            var sourceAssemblyName = Path.GetFileNameWithoutExtension(dll);
            var sourceDllName = Path.GetFileName(dll);            

            var tempPath = Path.Combine(Path.GetTempPath(), "SpecFlowFeatureAssemblyGenerator", Guid.NewGuid().ToString("N").Substring(0, 12));
            Directory.CreateDirectory(tempPath);
            CopyDirectory(sourceDirectory, tempPath);

            var specFlowProject = ReadSpecFlowProject(Path.Combine(tempPath, sourceDllName), defaultNamespace);
            var generator = SetupBatchGenerator(verboseOutput);
            generator.ProcessProject(specFlowProject, false /* forceGeneration */);
            

            try
            {
                Console.WriteLine("Generating source code using temporary path {0}", tempPath);

                var outputDirectory = Path.Combine(tempPath, "output");
                var featureAssemblyName = sourceAssemblyName + ".features";
                var featureDllName = featureAssemblyName + ".dll";
                Directory.CreateDirectory(outputDirectory);                

                GenerateFeatureAssembly(tempPath, outputDirectory, featureAssemblyName);
                GenerateConfiguration(Path.Combine(sourceDirectory, featureDllName + ".config"), new[] { sourceAssemblyName });                
                File.Copy(Path.Combine(outputDirectory, featureDllName), Path.Combine(sourceDirectory, featureDllName), true);
                File.Copy(Path.Combine(outputDirectory, featureAssemblyName + ".pdb"), Path.Combine(sourceDirectory, featureAssemblyName + ".pdb"), true);
                DeleteRedundantFiles(tempPath);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Successfully generated assembly {0}", featureDllName);
                Environment.Exit(0);
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Error.WriteLine("Could not generate assembly for the project.");

                Console.ForegroundColor = defaultColor;
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                Environment.Exit(-1);
                return;
            }
            finally
            {
                Console.ForegroundColor = defaultColor;
            }
        }


        static SpecFlowProject ReadSpecFlowProject(string assemblyFullPath, string defaultNameSpace = null)
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
                .Select(file => new FeatureFileInput(file) {
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

        static BatchGenerator SetupBatchGenerator(bool verboseOutput)
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
            generator.OnError += new Action<FeatureFileInput, TestGeneratorResult>(batchGenerator_OnError);
            return generator;
        }

        static void batchGenerator_OnError(FeatureFileInput featureFileInput, TestGeneratorResult testGeneratorResult)
        {
            Console.Error.WriteLine("Error generating for file {0}", featureFileInput.ProjectRelativePath);
            Console.Error.WriteLine(
                    testGeneratorResult.Errors
                    .Select(e => string.Format("Line {0}:{1} - {2}", e.Line, e.LinePosition, e.Message))
                    .JoinToString(Environment.NewLine)
                );
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
            if(results.Errors.Count > 0)
            {
                var messages = new List<string>();
                foreach(CompilerError err in results.Errors)
                {
                    if (!err.IsWarning)
                    {
                        messages.Add(err.ToString());
                    }
                }

                messages.Add(string.Empty);
                messages.Add("Output from the compiler:");
                foreach(string output in results.Output)
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

        static void CopyDirectory(string sourcePath, string destinationPath)
        {
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));

            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
        }

        static void DeleteRedundantFiles(string projectPath)
        {
            var reservedExtensions = new List<string>(new[] { "*.feature", "*.feature.cs" });
            Func<string, bool> shouldReserve = file => reservedExtensions.Any(extension => file.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase));

            Directory.GetFiles(projectPath, "*.*", SearchOption.AllDirectories)
                .WhereNot(shouldReserve)
                .ToList()
                .ForEach(File.Delete);
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
    }




    static class ArrayExtensions
    {
        public static string Get(this string[] args, int index)
        {
            return (args == null || (args.Length < index + 1)) ? null : args[index];
        }
    }

    static class EnumerableExtensions
    {
        public static string JoinToString<T>(this IEnumerable<T> list, string separator)
        {
            return string.Join<T>(separator, list);
        }

        public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> list, Func<T, bool> predicate)
        {
            return list.Where(item => !predicate(item));
        }
    }
}
