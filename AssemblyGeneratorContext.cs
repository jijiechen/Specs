using System.IO;

namespace generate_to_assembly
{
    class AssemblyGeneratorContext
    {
        public bool VerboseOutput { get; set; }
        public string DefaultNamespace { get; set; }

        public string SourcePath { get; set; }
        public SourceAssemblyProbe SourceAssembly { get; set; }


        public string FeatureAssemblyName
        {
            get { return SourceAssembly.AssemblyName.TryConcat(".features"); }
        }

        public string FeatureDllName
        {
            get { return FeatureAssemblyName.TryConcat(".dll"); }
        }


        public string TemporaryPath { get; set; }
        public string OutputPath
        {
            get { return TemporaryPath == null ? null : Path.Combine(TemporaryPath, "output"); }
        }
    }


    class SourceAssemblyProbe
    {
        public SourceAssemblyProbe(string assemblyPath, bool hasSpecFlowConfigured)
        {
            this.AssemblyPath = assemblyPath;
            this.AssemblyName = assemblyPath == null ? null : Path.GetFileNameWithoutExtension(assemblyPath);

            this.HasSpecFlowConfigured = hasSpecFlowConfigured;
        }

        public string AssemblyPath { get; }

        public string AssemblyName { get; }

        public bool HasSpecFlowConfigured { get;}
    }

}
