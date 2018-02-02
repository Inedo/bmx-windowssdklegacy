using System.ComponentModel;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Functions
{
    [ScriptAlias("MSBuildToolsPath")]
    [Description(@"The directory of the MSBuild tools, typically in 'C:\Program Files (x86)\MSBuild\{ToolsVersion}\Bin'; if empty, the MSBuildToolsPath registry value under SOFTWARE\Microsoft\MSBuild\ToolsVersions will be used.")]
    [Category("Server")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class MSBuildToolsPathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => string.Empty;
    }
}
