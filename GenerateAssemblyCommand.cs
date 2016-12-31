using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Linq;

namespace generate_to_assembly
{
    class GenerateAssemblyCommand
    {
        public static void Register(CommandLineApplication cmdApp)
        {
            cmdApp.Command("generate", command =>
            {
                command.Description = "Produce assembly for the project containing .feature cases.";


                var argProjectDir = command.Argument("[project]", "Optional. A folder path containing the project with .feature cases. The working directory will be used if not specified.", multipleValues: false);
                var optAssemblyName = command.Option("--assembly-name <ASSEMBLY_NAME>", "Optional. The name of the generated assembly. A random one will be used if not specified", CommandOptionType.SingleValue);
                var optDefaultNamespace = command.Option("--namespace <DEFAULT_NAMESPACE>", "The default namespace used to generate feature classes.", CommandOptionType.SingleValue);
                var optVerbose = command.Option("-v|--verbose", "Show verbose output", CommandOptionType.NoValue);

                command.HelpOption("-?|-h|--help");




                command.OnExecute(() =>
                {
                    var basePath = argProjectDir.Value;
                    basePath = Path.IsPathRooted(basePath) ? basePath : Environment.CurrentDirectory;

                    var context = new AssemblyGeneratorContext
                    {
                        VerboseOutput =  optVerbose.HasValue(),
                        DefaultNamespace = optDefaultNamespace.Value(),
                        SourcePath = basePath,
                        SpecifiedFeatureAssemblyName = optAssemblyName.Value(),
                        TemporaryPath = Path.Combine(Path.GetTempPath(), "SpecFlowFeatureAssemblyGenerator", "f" + Guid.NewGuid().ToString("N").Substring(0, 9))
                    };

                    return Run(context);
                });
            });
        }


        static int Run(AssemblyGeneratorContext context)
        {
            var defaultColor = Console.ForegroundColor;
            var exitCode = 0;

            try
            {
                if (context.VerboseOutput)
                {
                    Console.WriteLine("Generating source code using temporary path {0}", context.TemporaryPath);
                }
                Directory.CreateDirectory(context.TemporaryPath);
                Directory.CreateDirectory(context.OutputPath);
                DirectoryUtils.CopyDirectory(context.SourcePath, context.TemporaryPath);


                AssemblyGenerator.Generate(context, Console.Error);


                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Successfully generated assembly {0}", context.FeatureDllName);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Error.WriteLine("Could not generate assembly for the project.");
                Console.ForegroundColor = defaultColor;

                if (context.VerboseOutput)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                }
                exitCode = -1;
            }
            finally
            {
                Console.ForegroundColor = defaultColor;
            }

            return exitCode;
        }
        
    }
}
