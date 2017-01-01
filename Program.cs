using Microsoft.Extensions.CommandLineUtils;


namespace SpecsApp
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.Name = "specs";
            app.FullName = "Utility for generating and running assembly for SpecFlow feature files";
            app.HelpOption("-?|-h|--help");
            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 2;
            });

            Generating.GenerateAssemblyCommand.Register(app);


            return app.Execute(args);
        }
    }
}
