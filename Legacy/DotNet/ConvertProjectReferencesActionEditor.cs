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
        private FileBrowserTextBox libPath;
        private ValidatingTextBox searchMask;
        private CheckBox recursive;

        public override bool DisplaySourceDirectory => true;
        public override string SourceDirectoryLabel => "In:";
        public override string ServerLabel => "On:";

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
            this.libPath = new FileBrowserTextBox();
            this.searchMask = new ValidatingTextBox { Text = "*.csproj", TextMode = TextBoxMode.MultiLine, Rows = 4 };
            this.recursive = new CheckBox { Text = "Recursive" };

            this.Controls.Add(
                new SlimFormField("Library:", this.libPath),
                new SlimFormField("Project file masks:", this.searchMask),
                new SlimFormField("Options:", this.recursive)
            );
        }
    }
}
