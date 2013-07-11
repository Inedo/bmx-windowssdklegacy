using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Custom editor for the <see cref="AddComponentLicenseAction"/> class.
    /// </summary>
    internal sealed class AddComponentLicenseActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtComponents;
        private ValidatingTextBox txtProjects;
        private CheckBox chkRecursive;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddComponentLicenseActionEditor"/> class.
        /// </summary>
        public AddComponentLicenseActionEditor()
        {
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var action = (AddComponentLicenseAction)extension;
            this.txtComponents.Text = string.Join(Environment.NewLine, action.LicenesedComponents ?? new string[0]);
            this.txtProjects.Text = string.Join(Environment.NewLine, action.SearchMasks ?? new string[0]);
            this.chkRecursive.Checked = action.Recursive;
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new AddComponentLicenseAction
            {
                LicenesedComponents = this.txtComponents.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                SearchMasks = this.txtProjects.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                Recursive = this.chkRecursive.Checked
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtComponents = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Required = true,
                Width = 300,
                Rows = 5,
                Wrap = false
            };

            this.txtProjects = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Required = true,
                Width = 300,
                Rows = 5,
                Wrap = false
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Search directories recursively"
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Licensed Components",
                    "Specify the names of licensed components (one per line). These will be added as lines to a licenses.licx file.",
                    false,
                    new StandardFormField(
                        "Components:",
                        this.txtComponents
                    )
                ),
                new FormFieldGroup(
                    "Projects",
                    "Specify file masks (one per line) for project files which require the licensed components.",
                    true,
                    new StandardFormField(
                        "Project File Masks:",
                        this.txtProjects
                    ),
                    new StandardFormField(
                        string.Empty,
                        this.chkRecursive
                    )
                )
            );
        }
    }
}
