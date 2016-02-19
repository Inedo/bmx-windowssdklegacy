using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.WindowsSdk.Operations;

namespace Inedo.BuildMasterExtensions.WindowsSdk.ActionImporters
{
    internal sealed class SignExeImporter : IActionOperationConverter<SignExeAction, SignBinaryOperation>
    {
        public ConvertedOperation<SignBinaryOperation> ConvertActionToOperation(SignExeAction action, IActionConverterContext context)
        {
            return new SignBinaryOperation
            {
                Includes = new[] { action.SignExePath },
                ContentDescription = AH.NullIf(action.ContentDescription, string.Empty),
                ContentUrl = AH.NullIf(action.ContentUrl, string.Empty),
                SourceDirectory = AH.NullIf(action.OverriddenSourceDirectory, string.Empty),
                TimestampServer = AH.NullIf(action.TimestampServer, string.Empty),
                SubjectName = action.SubjectName
            };
        }
    }
}
