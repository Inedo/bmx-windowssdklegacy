using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class WriteAssemblyInfoVersionsActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtFileMasks;
        private CheckBox chkRecursive;
        private ValidatingTextBox txtVersion;

        public WriteAssemblyInfoVersionsActionEditor()
        {
        }

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var action = (WriteAssemblyInfoVersionsAction)extension;
            this.txtFileMasks.Text = string.Join(Environment.NewLine, action.FileMasks ?? new string[0]);
            this.chkRecursive.Checked = action.Recursive;
            this.txtVersion.Text = action.Version;
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new WriteAssemblyInfoVersionsAction
            {
                FileMasks = this.txtFileMasks.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                Recursive = this.chkRecursive.Checked,
                Version = this.txtVersion.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtFileMasks = new ValidatingTextBox
            {
                Required = true,
                TextMode = TextBoxMode.MultiLine,
                Width = 300,
                Rows = 5,
                Text = "*\\AssemblyInfo.cs"
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Also search in subdirectories"
            };

            this.txtVersion = new ValidatingTextBox
            {
                Width = 300,
                Required = true,
                Text = "%RELNO%.%BLDNO%"
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Files",
                    "Specify the masks (one per line) used to determine if a file should be searched for Assembly Version Attributes to replace.",
                    false,
                    new StandardFormField(
                        "File Masks:",
                        this.txtFileMasks
                    ),
                    new StandardFormField(
                        string.Empty,
                        this.chkRecursive
                    )
                ),
                new FormFieldGroup(
                    "Assembly Version",
                    "Specify the version to write to the matched Assembly Version Attributes.",
                    true,
                    new StandardFormField(
                        "Version:",
                        this.txtVersion
                    )
                )
            );
        }
    }
}
