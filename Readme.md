SpecFlow.FeatureAssemblyGenerator
----------------------

This is a tool that helps you generate SpecFlow feature test case code files into a single assembly instead of separated code files in your solution.

This project removes the dependency of project files(the .csproj) when generating SpecFlow code files, and this enables you the ability to run SpecFlow cases upon a packaged artifact.
Besides, it does not create any code file in the solution folder, so that your solution keeps clean.


**Usage**

It's a command line tool. It should be run after your main project which contains the step definitions has been compiled. To run it and generate the assembly to contain the fetaures, just restore NuGet packages, compile this project, and execute following command:  

```
specs.exe generate "path-to-directory-containing-step-assemblies" [--assembly-name name]
```


**Run Test Cases**

To run your cases(Gherkin), you should use the generated *`<AssemblyName>.features.dll`* assembly as the test assembly, and use your project as an external binding reference. 

To achieve this, you need to do following two steps:

* Specify the generated `.features.dll` assembly to be your test assembly. For example, if you use SpecRun as the runner, you need to add following configuration in the `.srprofile`:  
```xml
<TestAssemblyPath>AssemblyName.features.dll</TestAssemblyPath>
```
