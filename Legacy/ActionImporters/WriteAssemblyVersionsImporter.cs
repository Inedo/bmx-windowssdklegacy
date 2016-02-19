using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.WindowsSdk.DotNet;
using Inedo.BuildMasterExtensions.WindowsSdk.Operations.DotNet;

namespace Inedo.BuildMasterExtensions.WindowsSdk.ActionImporters
{
    internal sealed class WriteAssemblyVersionsImporter : IActionOperationConverter<WriteAssemblyInfoVersionsAction, WriteAssemblyInfoVersionsOperation>
    {
        public ConvertedOperation<WriteAssemblyInfoVersionsOperation> ConvertActionToOperation(WriteAssemblyInfoVersionsAction action, IActionConverterContext context)
        {
            var mask = context.ConvertLegacyMask(action.FileMasks, action.Recursive);
            return new WriteAssemblyInfoVersionsOperation
            {
                Includes = mask.Includes,
                Excludes = mask.Excludes,
                SourceDirectory = action.OverriddenSourceDirectory,
                Version = action.Version
            };
        }
    }
}
