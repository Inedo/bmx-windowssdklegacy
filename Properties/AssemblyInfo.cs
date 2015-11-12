using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMasterExtensions.WindowsSdk;

[assembly: AssemblyTitle("Windows SDK")]
[assembly: AssemblyDescription("Contains actions to build software targeting Windows or .NET platforms, including Build Project, MSBuild, Click Once, etc.")]

[assembly: ComVisible(false)]
[assembly: AssemblyCompany("Inedo, LLC")]
[assembly: AssemblyProduct("BuildMaster")]
[assembly: AssemblyCopyright("Copyright © 2008 - 2015")]
[assembly: AssemblyVersion("0.0.0.0")]
[assembly: AssemblyFileVersion("0.0")]
[assembly: CLSCompliant(false)]
[assembly: RequiredBuildMasterVersion("4.9.0")]
[assembly: ExtensionConfigurer(typeof(WindowsSdkExtensionConfigurer))]