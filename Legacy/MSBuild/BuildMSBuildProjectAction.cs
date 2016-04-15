using System;
using System.ComponentModel;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.ActionImporters;
using Inedo.BuildMasterExtensions.WindowsSdk.DotNet;
using Inedo.Documentation;
using Inedo.IO;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    [DisplayName("Build MSBuild Project")]
    [Description("Builds a project or solution using MSBuild.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(BuildMSBuildProjectActionEditor))]
    [ConvertibleToOperation(typeof(BuildProjectImporter))]
    public sealed class BuildMSBuildProjectAction : MSBuildActionBase
    {
        [Persistent]
        public string ProjectBuildConfiguration { get; set; }

        [Persistent]
        public string ProjectTargetPlatform { get; set; }

        [Persistent]
        public string ProjectPath { get; set; }

        [Persistent]
        public string MSBuildProperties { get; set; }

        [Persistent]
        public bool IsWebProject { get; set; }

        [Persistent]
        public bool BuildToProjectConfigSubdirectories { get; set; }

        [Persistent]
        public string AdditionalArguments { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            var projectPath = this.ProjectPath ?? string.Empty;

            var config = this.ProjectBuildConfiguration;
            if (!string.IsNullOrEmpty(this.ProjectTargetPlatform))
                config += "; " + this.ProjectTargetPlatform;

            DirectoryHilite targetHilite;
            if (this.BuildToProjectConfigSubdirectories)
            {
                if (projectPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                {
                    targetHilite = new DirectoryHilite(
                        this.OverriddenSourceDirectory,
                        PathEx.Combine(PathEx.GetDirectoryName(projectPath), @"{Project}\bin\" + this.ProjectBuildConfiguration)
                    );
                }
                else
                {
                    targetHilite = new DirectoryHilite(
                        this.OverriddenSourceDirectory,
                        PathEx.Combine(PathEx.GetDirectoryName(projectPath), @"bin\" + this.ProjectBuildConfiguration)
                    );
                }
            }
            else
            {
                targetHilite = new DirectoryHilite(this.OverriddenTargetDirectory);
            }

            return new ExtendedRichDescription(
                new RichDescription(
                    "Build ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, projectPath)
                ),
                new RichDescription(
                    "with ",
                    new Hilite(config),
                    " configuration to ",
                    targetHilite
                )
            );
        }

        protected override void Execute()
        {
            this.LogInformation("Building {0}...", this.ProjectPath);
            this.ExecuteRemoteCommand(null);
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var projectFullPath = Path.Combine(this.Context.SourceDirectory, this.ProjectPath);

            var buildProperties = string.Join(";", (this.MSBuildProperties ?? "").Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            var config = "Configuration=" + this.ProjectBuildConfiguration;
            if (!string.IsNullOrEmpty(this.ProjectTargetPlatform))
                config += ";Platform=" + this.ProjectTargetPlatform;

            if (!string.IsNullOrEmpty(buildProperties))
                config += ";" + buildProperties;

            try
            {
                DotNetHelper.EnsureMsBuildWebTargets();
            }
            catch
            {
            }

            this.LogDebug("Building {0}...", projectFullPath);

            int result = this.RunMSBuild(
                " \"{0}\" \"/p:{1}\"" + (this.IsWebProject || this.BuildToProjectConfigSubdirectories ? string.Empty : "  \"/p:OutDir={2}\\\""),
                projectFullPath,
                config,
                this.Context.TargetDirectory.EndsWith("\\")
                    ? this.Context.TargetDirectory
                    : this.Context.TargetDirectory + '\\'
            );

            if (result != 0)
            {
                this.LogError("Build failed (msbuild returned {0}).", result);
            }
            else if (this.IsWebProject)
            {
                result = this.RunMSBuild(
                    " \"{0}\" /target:_CopyWebApplication \"/p:OutDir={1}\\\" \"/p:WebProjectOutputDir={1}\\\" \"/p:{2}\"",
                    projectFullPath,
                    this.Context.TargetDirectory.EndsWith("\\")
                        ? this.Context.TargetDirectory
                        : this.Context.TargetDirectory + '\\',
                    config
                );

                if (result != 0)
                {
                    this.LogError("CopyWebApplication failed (msbuild returned {0}).", result);
                }
                else
                {
                    result = this.RunMSBuild(" \"{0}\" /target:ResolveReferences \"/property:OutDir={1}\\\" \"/p:{2}\"",
                        projectFullPath,
                        Path.Combine(this.Context.TargetDirectory, "bin") + '\\',
                        config
                    );

                    if (result != 0)
                        this.LogError("ResolveReferences failed (msbuild returned {0}).", result);
                }
            }

            return result.ToString();
        }

        private int RunMSBuild(string argsFormat, params string[] args)
        {
            var allArgs = string.Format(argsFormat, args);

            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                allArgs += " " + this.AdditionalArguments;

            var workingDir = PathEx.Combine(
                this.Context.SourceDirectory,
                Path.GetDirectoryName(this.ProjectPath)
            );

            if (!Directory.Exists(workingDir))
                throw new DirectoryNotFoundException(string.Format("Directory {0} does not exist.", workingDir));

            return this.InvokeMSBuild(allArgs, workingDir);
        }
    }
}
