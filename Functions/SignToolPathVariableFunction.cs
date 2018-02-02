using System.ComponentModel;
using Inedo.Extensibility;
using Inedo.Extensibility.VariableFunctions;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Functions
{
    [ScriptAlias("SignToolPath")]
    [Description("The full path to signtool.exe; if empty will attempt to resolve the path automatically.")]
    [Category("Server")]
    [ExtensionConfigurationVariable(Required = false)]
    public sealed class SignToolPathVariableFunction : ScalarVariableFunction
    {
        protected override object EvaluateScalar(IVariableFunctionContext context) => string.Empty;
    }
}
