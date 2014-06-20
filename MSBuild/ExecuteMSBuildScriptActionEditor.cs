using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    internal sealed class ExecuteMSBuildScriptActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtProjectFilePath, txtMSBuildTarget, txtAdditionalProperties;
        private DropDownList ddlVersion;

        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }
        public override string TargetDirectoryLabel
        {
            get { return "To:"; }
        }

        protected override void CreateChildControls()
        {
            this.ddlVersion = new DropDownList();
            this.ddlVersion.Items.Add(new ListItem("(auto detect)", ""));
            this.ddlVersion.Items.Add(new ListItem("2.0", "2.0.50727"));
            this.ddlVersion.Items.Add(new ListItem("3.5", "3.5"));
            this.ddlVersion.Items.Add(new ListItem("4.0", "4.0.30319"));

            this.txtProjectFilePath = new ValidatingTextBox();

            this.txtMSBuildTarget = new ValidatingTextBox { Required = true };

            this.txtAdditionalProperties = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            this.Controls.Add(
                new SlimFormField(".NET version:", this.ddlVersion),
                new SlimFormField("MSBuild file:", this.txtProjectFilePath),
                new SlimFormField("MSBuild target:", this.txtMSBuildTarget),
                new SlimFormField("MSBuild properties:", this.txtAdditionalProperties)
                {
                    HelpText = HelpText.FromHtml("Additional properties, separated by newlines. Example:<br />WarningLevel=2<br />Optimize=false")
                }
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            var buildAction = (ExecuteMSBuildScriptAction)extension;
            this.txtProjectFilePath.Text = Util.Path2.Combine(buildAction.OverriddenSourceDirectory, buildAction.MSBuildPath);
            this.txtMSBuildTarget.Text = buildAction.ProjectBuildTarget;
            this.txtAdditionalProperties.Text = buildAction.MSBuildProperties;
            this.ddlVersion.SelectedValue = buildAction.DotNetVersion ?? string.Empty;
        }

        public override ActionBase CreateFromForm()
        {
            var buildProperties = this.txtAdditionalProperties.Text;
            if (buildProperties.StartsWith("/p:"))
                buildProperties = buildProperties.Replace("/p:", string.Empty);

            return new ExecuteMSBuildScriptAction
            {
                OverriddenSourceDirectory = Util.Path2.GetDirectoryName(this.txtProjectFilePath.Text),
                MSBuildPath = Util.Path2.GetFileName(this.txtProjectFilePath.Text),
                ProjectBuildTarget = this.txtMSBuildTarget.Text,
                MSBuildProperties = buildProperties,
                DotNetVersion = this.ddlVersion.SelectedValue
            };
        }
    }
}
