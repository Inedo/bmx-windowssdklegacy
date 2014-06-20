using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class ConvertProjectReferencesActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker libPath;
        private ValidatingTextBox searchMask;
        private CheckBox recursive;

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
            var convert = (ConvertProjectReferencesAction)extension;
            this.libPath.Text = convert.LibraryPath;
            this.searchMask.Text = string.Join(Environment.NewLine, convert.SearchMasks);
            this.recursive.Checked = convert.Recursive;
        }

        public override ActionBase CreateFromForm()
        {
            return new ConvertProjectReferencesAction()
            {
                LibraryPath = this.libPath.Text,
                SearchMasks = Regex.Split(this.searchMask.Text, "\r?\n"),
                Recursive = this.recursive.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.libPath = new SourceControlFileFolderPicker { DisplayMode = SourceControlBrowser.DisplayModes.Folders };
            this.searchMask = new ValidatingTextBox { Text = "*.csproj", TextMode = TextBoxMode.MultiLine, Rows = 4 };
            this.recursive = new CheckBox { Text = "Recursive" };

            this.Controls.Add(
                new FormFieldGroup(
                    "Library",
                    "The library directory which contains referenced assemblies.",
                    false,
                    new StandardFormField(string.Empty, this.libPath)
                ),
                new FormFieldGroup(
                    "Project Files",
                    "Determines which project files are converted.",
                    true,
                    new StandardFormField("File Masks:", this.searchMask),
                    new StandardFormField(string.Empty, this.recursive)
                )
            );
        }
    }
}
