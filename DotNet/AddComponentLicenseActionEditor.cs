using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class AddComponentLicenseActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtComponents;
        private ValidatingTextBox txtProjects;
        private CheckBox chkRecursive;

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }
        public override string SourceDirectoryLabel
        {
            get { return "In:"; }
        }
        public override string ServerLabel
        {
            get { return "On:"; }
        }

        public override void BindToForm(ActionBase extension)
        {
            var action = (AddComponentLicenseAction)extension;
            this.txtComponents.Text = string.Join(Environment.NewLine, action.LicenesedComponents ?? new string[0]);
            this.txtProjects.Text = string.Join(Environment.NewLine, action.SearchMasks ?? new string[0]);
            this.chkRecursive.Checked = action.Recursive;
        }
        public override ActionBase CreateFromForm()
        {
            return new AddComponentLicenseAction
            {
                LicenesedComponents = this.txtComponents.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                SearchMasks = this.txtProjects.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                Recursive = this.chkRecursive.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtComponents = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Required = true,
                Rows = 5,
                Wrap = false
            };

            this.txtProjects = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Required = true,
                Rows = 5,
                Wrap = false
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Search directories recursively"
            };

            this.Controls.Add(
                new SlimFormField("Licensed components:", this.txtComponents)
                {
                    HelpText = "Specify the names of licensed components (one per line). These will be added as lines to a licenses.licx file."
                },
                new SlimFormField("Projects:", this.txtProjects)
                {
                    HelpText =  "Specify file masks (one per line) for project files which require the licensed components."
                },
                new SlimFormField("Options:", this.chkRecursive)
            );
        }
    }
}
