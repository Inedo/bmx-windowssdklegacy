using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                ContentDescription = InedoLib.Util.NullIf(action.ContentDescription, string.Empty),
                ContentUrl = InedoLib.Util.NullIf(action.ContentUrl, string.Empty),
                SourceDirectory = InedoLib.Util.NullIf(action.OverriddenSourceDirectory, string.Empty),
                TimestampServer = InedoLib.Util.NullIf(action.TimestampServer, string.Empty),
                SubjectName = action.SubjectName
            };
        }
    }
}
