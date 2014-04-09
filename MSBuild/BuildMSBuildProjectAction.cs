﻿using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.DotNet;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    /// <summary>
    /// Represents an action that builds a .NET application.
    /// </summary>
    [ActionProperties(
        "Build MSBuild Project",
        "Builds a project or solution using MSBuild.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(BuildMSBuildProjectActionEditor))]
    public sealed class BuildMSBuildProjectAction : MSBuildActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildMSBuildProjectAction"/> class.
        /// </summary>
        public BuildMSBuildProjectAction()
        {
        }

        /// <summary>
        /// Gets or sets the project's build configuration (generally, Debug or Release)
        /// </summary>
        [Persistent]
        public string ProjectBuildConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the project's target platform (generally AnyCPU or x86).
        /// </summary>
        [Persistent]
        public string ProjectTargetPlatform { get; set; }

        /// <summary>
        /// Gets or sets the absolute path the project file
        /// </summary>
        /// <example>
        /// c:\Build\myproj\myproj.cs
        /// </example>
        [Persistent]
        public string ProjectPath { get; set; }

        /// <summary>
        /// Gets or sets the project's properties for the msbuild script
        /// </summary>
        [Persistent]
        public string MSBuildProperties { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a web project.
        /// </summary>
        /// <value>
        /// <c>true</c> if this is a web project; otherwise, <c>false</c>.
        /// </value>
        [Persistent]
        public bool IsWebProject { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [build to project config subdirectories].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [build to project config subdirectories]; otherwise, <c>false</c>.
        /// </value>
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
                    new DirectoryHilite(this.OverriddenSourceDirectory, projectPath),
                    " (",
                    new Hilite(config),
                    ")"
                ),
                new LongActionDescription(
                    "to ",
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
