using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    internal sealed class BuildAspNetProjectActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtProjectBuildConfiguration;
        private SourceControlFileFolderPicker txtProjectPath;
        private ValidatingTextBox txtAdditionalArguments;

        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }
        public override string TargetDirectoryLabel
        {
            get { return "To:"; }
        }

        public override void BindToForm(ActionBase extension)
        {
            var action = (BuildAspNetProjectAction)extension;
            this.txtProjectBuildConfiguration.Text = action.ProjectBuildConfiguration;
            this.txtProjectPath.Text = PathEx.Combine(action.OverriddenSourceDirectory, action.ProjectPath);
            this.txtAdditionalArguments.Text = action.AdditionalArguments;
        }

        public override ActionBase CreateFromForm()
        {
            return new BuildAspNetProjectAction
            {
                ProjectBuildConfiguration = this.txtProjectBuildConfiguration.Text,
                ProjectPath = PathEx.GetFileName(this.txtProjectPath.Text),
                AdditionalArguments = this.txtAdditionalArguments.Text,
                OverriddenSourceDirectory = PathEx.GetDirectoryName(this.txtProjectPath.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtProjectBuildConfiguration = new ValidatingTextBox
            {
                ID = "txtProjectBuildConfiguration",
                AutoCompleteValues = new[] { "Debug", "Release" },
                Required = true,
                Text = "Release"
            };

            this.txtProjectPath = new SourceControlFileFolderPicker
            {
                ID = "txtProjectPath",
                Required = true
            };

            this.txtAdditionalArguments = new ValidatingTextBox
            {
                ID = "txtAdditionalArguments",
                DefaultText = "(none)"
            };

            this.Controls.Add(
                new SlimFormField("Project file:", this.txtProjectPath),
                new SlimFormField("Configuration:", this.txtProjectBuildConfiguration),
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments)
            );
        }
    }
}
