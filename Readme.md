Specs
--------

This is a tool that helps you generate SpecFlow feature test case code files into a single assembly instead of separated code files in your solution.
It also contains the SpecRun evaluation mode runner to help you quick start your specification project.

This project removes the dependency of project files(the .csproj) when generating SpecFlow code files which keeps your solution clean; it enables you the ability to run SpecFlow cases upon a packaged artifact.


##Usage

It's a command line tool, and can be used to generate a feature assembly or just run the specs.

###Generating the feature assembly
After your main project which contains the step definitions has been compiled(built), this tool will help generate a feature assembly for the `.feature` files in your project. After having the feature assembly generated, use your favorite runner to run the cases. This project also has a runner builtin, see below.

To generate the feature assembly, restore NuGet packages, compile the `SpecsApp` project, and execute following command:  
```
specs.exe generate "path-to-directory-containing-step-assemblies" [--assembly-name name]
```


###Run Test Cases

This project also provides a all-in-one batch file which contains the [SpecRun](http://specflow.org/plus/runner/) evaluation mode runner to help you build and run your specs project quickly, without any interaction.
May it help you quick start your specification project.
```
cd your-project
run-specs.bat [fixed-assembly-name]
```
The project can be a source project or a built artifact directory.

To run your cases(Gherkin) manually, you should use the generated *`<AssemblyName>.features.dll`* assembly as the test assembly, and use your project as an external binding reference. 

For example, for a SpecRun runner, you configure in the `.srprofile` like this:

```xml
<TestAssemblyPath>AssemblyName.features.dll</TestAssemblyPath>
```


**.NET Core**
This project does not support .NET Core yet. As SpecFlow is working on .NET Core Support, so this project will follow up if necessary.
