using System;
using System.ComponentModel;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    [DisplayName("Execute MSBuild Script")]
    [Description("Executes an .msbuild script file.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(ExecuteMSBuildScriptActionEditor))]
    public sealed class ExecuteMSBuildScriptAction : MSBuildActionBase
    {
        public ExecuteMSBuildScriptAction()
        {
            this.MSBuildProperties = string.Empty;
        }

        [Persistent]
        public string ProjectBuildTarget { get; set; }

        [Persistent]
        public string MSBuildPath { get; set; }

        [Persistent]
        public string MSBuildProperties { get; set; }

        [Persistent]
        public string AdditionalArguments { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "MSBuild ",
                    new Hilite(this.ProjectBuildTarget),
                    " ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.MSBuildPath)
                ),
                new RichDescription(
                    "with properties ",
                    new ListHilite((this.MSBuildProperties ?? string.Empty).Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                )
            );
        }

        protected override void Execute()
        {
            this.LogInformation("Executing {0}...", this.MSBuildPath);
            this.ExecuteRemoteCommand(null);
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var projectFileName = Path.Combine(this.Context.SourceDirectory, this.MSBuildPath);

            var buildProperties = string.Join(";", this.MSBuildProperties.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            //Execute msbuild script
            //Format: MSBuild {projectFileName} /t:{ProjectBuildTarget}
            int result = this.InvokeMSBuild(
                string.Format(
                    " \"{0}\" \"/t:{1}\" \"/p:outDir={2}{3}\" {4}",
                    projectFileName,
                    this.ProjectBuildTarget,
                    this.Context.TargetDirectory.EndsWith("\\") ?
                        this.Context.TargetDirectory :
                        this.Context.TargetDirectory + "\\",
                    Util.ConcatNE(";", buildProperties),
                    Util.ConcatNE("\"", this.AdditionalArguments, "\"")
                ),
                this.Context.SourceDirectory
            );

            if (result != 0)
                this.LogError("MSBuild failed with code {0}.", result);

            return result.ToString();
        }
    }
}
