using System;
using System.IO;


namespace generate_to_assembly
{
    class Program
    {
        
        static void Main(string[] args)
        {
            var defaultColor = Console.ForegroundColor;
            var exitCode = 0;
            var context = InitContextFromCommandlineArguments(args);

            try
            {
                Console.WriteLine("Generating source code using temporary path {0}", context.TemporaryPath);
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
                Console.Error.WriteLine(ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                exitCode = -1;
            }
            finally
            {
                Console.ForegroundColor = defaultColor;
            }

            Environment.Exit(exitCode);
        }

        static AssemblyGeneratorContext InitContextFromCommandlineArguments(string[] args)
        {
            var basePath = args.Get(0);
            basePath = Path.IsPathRooted(basePath) ? basePath : Environment.CurrentDirectory;

            // System.Diagnostics.Debugger.Launch();

            var context = new AssemblyGeneratorContext
            {
                VerboseOutput = true,
                DefaultNamespace = args.Get(1),
                SourcePath = basePath,
                TemporaryPath = Path.Combine(Path.GetTempPath(), "SpecFlowFeatureAssemblyGenerator", "f" + Guid.NewGuid().ToString("N").Substring(0, 9))
            };
            return context;
        }
    }
}
