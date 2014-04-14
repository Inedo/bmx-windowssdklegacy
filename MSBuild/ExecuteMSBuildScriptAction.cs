using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    /// <summary>
    /// Represents an action that builds an MSBuild project.
    /// </summary>
    [ActionProperties(
        "Execute MSBuild Script",
        "Executes an .msbuild script file.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(ExecuteMSBuildScriptActionEditor))]
    public sealed class ExecuteMSBuildScriptAction : MSBuildActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteMSBuildScriptAction"/> class.
        /// </summary>
        public ExecuteMSBuildScriptAction()
        {
            this.MSBuildProperties = string.Empty;
        }

        /// <summary>
        /// Gets or sets the project's target for the msbuild script
        /// </summary>
        [Persistent]
        public string ProjectBuildTarget { get; set; }

        /// <summary>
        /// Gets or sets the project's path for the msbuild script
        /// </summary>
        [Persistent]
        public string MSBuildPath { get; set; }

        /// <summary>
        /// Gets or sets the project's properties for the msbuild script
        /// </summary>
        [Persistent]
        public string MSBuildProperties { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "MSBuild ",
                    new Hilite(this.ProjectBuildTarget),
                    " ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.MSBuildPath)
                ),
                new LongActionDescription(
                    "with properties ",
                    new ListHilite((this.MSBuildProperties ?? string.Empty).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                )
            );
        }

        protected override void Execute()
        {
            var retval = ExecuteRemoteCommand(null);
            if (retval != "0")
                this.LogError("MSBuild action failed; msbuild.exe returned code " + retval);
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var projectFileName = Path.Combine(this.Context.SourceDirectory, this.MSBuildPath);

            // Parse build properties from:
            //      prop1=val1
            //      prop2=val2
            // to:
            //      prop1=val1;prop2=val2
            var buildProperties = string.Join(
                ";",
                this.MSBuildProperties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            );

            //Execute msbuild script
            //Format: MSBuild {projectFileName} /t:{ProjectBuildTarget}
            return this.InvokeMSBuild(
                string.Format(
                    " \"{0}\" \"/t:{1}\" \"/p:outDir={2}{3}\"",
                    projectFileName,
                    this.ProjectBuildTarget,
                    this.Context.TargetDirectory.EndsWith("\\") ?
                        this.Context.TargetDirectory :
                        this.Context.TargetDirectory + "\\",
                    Util.ConcatNE(";", buildProperties)
                ),
                this.Context.SourceDirectory
            )
            .ToString();
        }
    }
}
