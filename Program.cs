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


                AssemblyGenerator.Generate(context);

                
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
            var defaultNamespace = args.Get(1);
            var dll = args.Get(0);
            if (dll == null)
            {
                throw new ApplicationException("Please use first parameter to represent assembly path.");
            }
            dll = Path.IsPathRooted(dll) ? dll : Path.Combine(Environment.CurrentDirectory, dll);

            // System.Diagnostics.Debugger.Launch();

            var context = new AssemblyGeneratorContext
            {
                VerboseOutput = true,
                DefaultNamespace = defaultNamespace,
                SourcePath = Path.GetDirectoryName(dll),
                SourceAssemblyName = Path.GetFileNameWithoutExtension(dll),
                TemporaryPath = Path.Combine(Path.GetTempPath(), "SpecFlowFeatureAssemblyGenerator", Guid.NewGuid().ToString("N").Substring(0, 12))
            };
            return context;
        }
    }
}
