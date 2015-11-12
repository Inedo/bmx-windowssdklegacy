using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    internal sealed class ExecuteMSBuildScriptActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker txtProjectFilePath;
        private ValidatingTextBox txtMSBuildTarget;
        private ValidatingTextBox txtAdditionalProperties;
        private ValidatingTextBox txtAdditionalArguments;

        public override bool DisplayTargetDirectory => true;
        public override string TargetDirectoryLabel => "To:";

        protected override void CreateChildControls()
        {
            this.txtProjectFilePath = new SourceControlFileFolderPicker
            {
                ID = "txtProjectFilePath",
                Required = true
            };

            this.txtMSBuildTarget = new ValidatingTextBox
            {
                ID = "txtMSBuildTarget",
                Required = true
            };

            this.txtAdditionalProperties = new ValidatingTextBox
            {
                ID = "txtAdditionalProperties",
                TextMode = TextBoxMode.MultiLine,
                DefaultText = "(none)",
                Rows = 5
            };

            this.txtAdditionalArguments = new ValidatingTextBox
            {
                ID = "txtAdditionalArguments",
                DefaultText = "(none)"
            };

            this.Controls.Add(
                new SlimFormField("MSBuild file:", this.txtProjectFilePath),
                new SlimFormField("MSBuild target:", this.txtMSBuildTarget),
                new SlimFormField("MSBuild properties:", this.txtAdditionalProperties)
                {
                    HelpText = HelpText.FromHtml("Additional properties, separated by newlines. Example:<br />WarningLevel=2<br />Optimize=false")
                },
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments)
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            var buildAction = (ExecuteMSBuildScriptAction)extension;
            this.txtProjectFilePath.Text = PathEx.Combine(buildAction.OverriddenSourceDirectory, buildAction.MSBuildPath);
            this.txtMSBuildTarget.Text = buildAction.ProjectBuildTarget;
            this.txtAdditionalProperties.Text = buildAction.MSBuildProperties;
            this.txtAdditionalArguments.Text = buildAction.AdditionalArguments;
        }

        public override ActionBase CreateFromForm()
        {
            var buildProperties = this.txtAdditionalProperties.Text;
            if (buildProperties.StartsWith("/p:"))
                buildProperties = buildProperties.Replace("/p:", string.Empty);

            return new ExecuteMSBuildScriptAction
            {
                OverriddenSourceDirectory = PathEx.GetDirectoryName(this.txtProjectFilePath.Text),
                MSBuildPath = PathEx.GetFileName(this.txtProjectFilePath.Text),
                ProjectBuildTarget = this.txtMSBuildTarget.Text,
                MSBuildProperties = buildProperties,
                AdditionalArguments = this.txtAdditionalArguments.Text
            };
        }
    }
}
