using Mono.Cecil;
using System.IO;
using System.Linq;

namespace generate_to_assembly
{
    class AssemblyReflecter
    {

        public static LoadedAssembly[] ProbeReferenceAssemblies(string referencePath)
        {
            var exes = Directory.GetFiles(referencePath, "*.exe", SearchOption.TopDirectoryOnly);
            var dlls = Directory.GetFiles(referencePath, "*.dll", SearchOption.TopDirectoryOnly);

            return exes.Concat(dlls)
                .Where(f => !f.EndsWith(".features.dll"))
                .Select(ReadAssembly)
                .Where(assembly => assembly.Definition != null)
                .ToArray();
        }

        static LoadedAssembly ReadAssembly(string assemblyPath)
        {
            var assembly = new LoadedAssembly
            {
                FullPath = assemblyPath
            };


            try
            {
                assembly.Definition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadSymbols = false });
            }
            catch { }

            return assembly;
        }

        public static string[] GetStepAssemblyNames(LoadedAssembly[] referenceAssemblies, string basePath)
        {
            return referenceAssemblies
                    .Where(IsStepAssembly)
                    .Select(assembly => Path.Combine(Path.GetDirectoryName(assembly.FullPath), Path.GetFileNameWithoutExtension(assembly.FullPath)))
                    .Select(path => path.Replace(basePath, string.Empty).TrimStart(Path.DirectorySeparatorChar))
                    .ToArray();
        }

        static bool IsStepAssembly(LoadedAssembly assembly)
        {
            try
            {
                return assembly.Definition.Modules
                    .SelectMany(m => m.Types)
                    .Where(t => t.IsPublic && !t.IsAbstract && !t.IsInterface)
                    .Where(t => t.CustomAttributes.Any(attr => attr.AttributeType.FullName == "TechTalk.SpecFlow.BindingAttribute"))
                    .Any();
            }
            catch
            {
                return false;
            }
        }
    }

    struct LoadedAssembly
    {
        public string FullPath { get; set; }
        public AssemblyDefinition Definition { get; set; }
    }
}
