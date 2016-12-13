SpecFlow.FeatureAssemblyGenerator
----------------------

This is a tool that helps you generate SpecFlow feature test case code files into a single assembly instead of separated code files in your solution.

This project removes the dependency of project files(the .csproj) when generating SpecFlow code files, and this enables you the ability to run SpecFlow cases upon a packaged artifact.
Besides, it does not create any code file in the solution folder, so that your solution keeps clean.


**Notes**
To run the cases, you should use the generated *`<AssemblyName>.features.dll`* assembly as the test assembly, and use your project as an external binding reference. 

To achieve this, you need to do following two steps:

* Add an extra SpecFlow configuration file *`<AssemblyName>.features.dll.config`* and set its `CopyLocal` to `true`  in your solution to configure the generated assembly. In that configuration file, reference your project with `stepAssembly` sections.  Please refer to [this documentation](http://specflow.org/documentation/Use-Bindings-from-External-Assemblies/) to see how to configure an external binding assembly.


* Specify the generated `.features.dll` assembly to be your test assembly. For example, if you use SpecRun to be the runner, we need to add following configuration in the profile:  
```xml
    <TestAssemblyPath>MySpecs.features.dll</TestAssemblyPath>
```

