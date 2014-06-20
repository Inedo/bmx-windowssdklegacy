using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.DotNet;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    [ActionProperties(
        "Build MSBuild Project",
        "Builds a project or solution using MSBuild.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(BuildMSBuildProjectActionEditor))]
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

        public override ActionDescription GetActionDescription()
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
                        Util.Path2.Combine(Util.Path2.GetDirectoryName(projectPath), @"{Project}\bin\" + this.ProjectBuildConfiguration)
                    );
                }
                else
                {
                    targetHilite = new DirectoryHilite(
                        this.OverriddenSourceDirectory,
                        Util.Path2.Combine(Util.Path2.GetDirectoryName(projectPath), @"bin\" + this.ProjectBuildConfiguration)
                    );
                }
            }
            else
            {
                targetHilite = new DirectoryHilite(this.OverriddenTargetDirectory);
            }

            return new ActionDescription(
                new ShortActionDescription(
                    "Build ",
                    new Hilite(projectPath),
                    " (",
                    new Hilite(config),
                    ")"
                ),
                new LongActionDescription(
                    "from ",
                    new DirectoryHilite(this.OverriddenSourceDirectory),
                    " to ",
                    targetHilite
                )
            );
        }

        protected override void Execute()
        {
            var retVal = string.Empty;

            this.LogDebug("Building Application");
            retVal = this.ExecuteRemoteCommand("Build");
            if (retVal != "0")
            {
                this.LogError("Step failed (msbuild returned code {0})", retVal);
                return;
            }

            if (this.IsWebProject)
            {
                this.LogDebug("Copying Web Files");
                retVal = this.ExecuteRemoteCommand("CopyWeb");
                if (retVal != "0")
                {
                    this.LogError("Step failed (msbuild returned code {0})", retVal);
                    return;
                }

                this.LogDebug("Copying References");
                retVal = this.ExecuteRemoteCommand("CopyRef");
                if (retVal != "0")
                {
                    this.LogError("Step failed (msbuild returned code {0})", retVal);
                    return;
                }
            }

        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var projectFullPath = Path.Combine(this.Context.SourceDirectory, this.ProjectPath);

            var buildProperties =
                string.Join(
                    ";",
                    (this.MSBuildProperties ?? "").Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            );

            LogDebug(string.Format("  Action: {0}; Path: {1}", name, projectFullPath));

            var config = "Configuration=" + this.ProjectBuildConfiguration;
            if (!string.IsNullOrEmpty(this.ProjectTargetPlatform))
                config += ";Platform=" + this.ProjectTargetPlatform;

            if (!string.IsNullOrEmpty(buildProperties))
                config += ";" + buildProperties;

            switch (name)
            {
                case "Build":
                    try
                    {
                        DotNetHelper.EnsureMsBuildWebTargets();
                    }
                    catch
                    {
                        //gulp
                    }
                    return msbuild(" \"{0}\" \"/p:{1}\""
                        + (this.IsWebProject || this.BuildToProjectConfigSubdirectories ? "" : "  \"/p:OutDir={2}\\\""),
                        projectFullPath,
                        config,
                        this.Context.TargetDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())
                            ? this.Context.TargetDirectory
                            : this.Context.TargetDirectory + Path.DirectorySeparatorChar
                        );

                case "CopyWeb":
                    return msbuild(" \"{0}\" /target:_CopyWebApplication \"/p:OutDir={1}\\\" \"/p:WebProjectOutputDir={1}\\\" \"/p:{2}\"",
                        projectFullPath,
                        this.Context.TargetDirectory.EndsWith(Path.DirectorySeparatorChar.ToString())
                            ? this.Context.TargetDirectory
                            : this.Context.TargetDirectory + Path.DirectorySeparatorChar,
                        config);
                    
                case "CopyRef":
                    return msbuild(" \"{0}\" /target:ResolveReferences \"/property:OutDir={1}\\\" \"/p:{2}\"",
                        projectFullPath,
                        Path.Combine(this.Context.TargetDirectory, "bin" + Path.DirectorySeparatorChar),
                        config);
                    
                default:
                    throw new ArgumentOutOfRangeException("name");
            }
        }

        private string msbuild(string argsFormat, params string[] args)
        {
            var allArgs = string.Format(argsFormat, args);

            var workingDir = Path.Combine(
                this.Context.SourceDirectory,
                Path.GetDirectoryName(this.ProjectPath)
            );

            if (!Directory.Exists(workingDir))
                throw new DirectoryNotFoundException(string.Format("Directory {0} does not exist.", workingDir));

            return this.InvokeMSBuild(
                allArgs,
                workingDir
            )
            .ToString();
        }
    }
}
