using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;
using Inedo.BuildMasterExtensions.WindowsSdk.Operations.MSBuild;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.WindowsSdk.ActionImporters
{
    internal sealed class BuildProjectImporter : IActionOperationConverter<BuildMSBuildProjectAction, BuildMSBuildProjectOperation>
    {
        public ConvertedOperation<BuildMSBuildProjectOperation> ConvertActionToOperation(BuildMSBuildProjectAction action, IActionConverterContext context)
        {
            var properties = action.MSBuildProperties?.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (properties != null && properties.Length == 0)
                properties = null;

            return new BuildMSBuildProjectOperation
            {
                ProjectPath = !string.IsNullOrEmpty(action.OverriddenSourceDirectory) ? PathEx.Combine(action.OverriddenSourceDirectory, action.ProjectPath) : action.ProjectPath,
                TargetDirectory = !action.BuildToProjectConfigSubdirectories ? Util.CoalesceStr(action.OverriddenTargetDirectory, "$CurrentDirectory") : null,
                BuildConfiguration = action.ProjectBuildConfiguration,
                TargetPlatform = action.ProjectTargetPlatform,
                MSBuildProperties = properties,
                AdditionalArguments = action.AdditionalArguments,
                MSBuildToolsPath = "$MSBuildToolsPath"
            };
        }
    }
}
