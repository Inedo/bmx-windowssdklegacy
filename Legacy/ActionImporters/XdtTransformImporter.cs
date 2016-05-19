using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.WindowsSdk.Operations;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.WindowsSdk.ActionImporters
{
    internal sealed class XdtTransformImporter : IActionOperationConverter<XdtTransformAction, XdtTransformOperation>
    {
        public ConvertedOperation<XdtTransformOperation> ConvertActionToOperation(XdtTransformAction action, IActionConverterContext context)
        {
            string source = PathEx.Combine(action.OverriddenSourceDirectory, action.SourceFile);
            string dest = PathEx.Combine(action.OverriddenTargetDirectory, action.DestinationFile);
            if (source == dest)
                dest = null;

            return new XdtTransformOperation
            {
                SourceFile = source,
                DestinationFile = dest,
                PreserveWhitespace = action.PreserveWhitespace,
                TransformFile = action.TransformFile,
                Verbose = action.Verbose
            };
        }
    }
}
