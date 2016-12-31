using System.IO;

namespace generate_to_assembly
{
    public class AssemblyGeneratorContext
    {
        public bool VerboseOutput { get; set; }
        public string DefaultNamespace { get; set; }

        public string SourcePath { get; set; }
        public string SourceAssemblyName { get; set; }
        public string SourceDllName
        {
            get { return SourceAssemblyName.TryConcat(".dll"); }
        }

        public string FeatureAssemblyName
        {
            get { return SourceAssemblyName.TryConcat(".features"); }
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

}
