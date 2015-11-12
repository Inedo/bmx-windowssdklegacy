using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    internal sealed class XdtTransformActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtSourceFile;
        private ValidatingTextBox txtTransformFile;
        private ValidatingTextBox txtDestinationFile;
        private CheckBox chkPreserveWhitespace;
        private CheckBox chkVerbose;

        public override string ServerLabel => "On:";

        public override void BindToForm(ActionBase extension)
        {
            var action = (XdtTransformAction)extension;

            this.txtSourceFile.Text = PathEx.Combine(action.OverriddenSourceDirectory, action.SourceFile);
            this.txtTransformFile.Text = action.TransformFile;
            this.txtDestinationFile.Text = Path.Combine(action.OverriddenTargetDirectory, action.DestinationFile);
            this.chkPreserveWhitespace.Checked = action.PreserveWhitespace;
            this.chkVerbose.Checked = action.Verbose;
        }

        public override ActionBase CreateFromForm()
        {
            return new XdtTransformAction
            {
                OverriddenSourceDirectory = PathEx.GetDirectoryName(this.txtSourceFile.Text),
                SourceFile = PathEx.GetFileName(this.txtSourceFile.Text),
                TransformFile = this.txtTransformFile.Text,
                DestinationFile = PathEx.GetFileName(this.txtDestinationFile.Text),
                OverriddenTargetDirectory = PathEx.GetDirectoryName(this.txtDestinationFile.Text),
                PreserveWhitespace = this.chkPreserveWhitespace.Checked,
                Verbose = this.chkVerbose.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtSourceFile = new ValidatingTextBox { Required = true, Text = "Web.config" };
            this.txtTransformFile = new ValidatingTextBox { Required = true };
            this.txtDestinationFile = new ValidatingTextBox { Required = true, Text = "Web.config" };
            this.chkPreserveWhitespace = new CheckBox { Text = "Preserve Whitespace in Destination File", Checked = true };
            this.chkVerbose = new CheckBox { Text = "Enable Verbose Logging", Checked = true };

            this.Controls.Add(
                new SlimFormField("Source file:", this.txtSourceFile),
                new SlimFormField("Transform file:", this.txtTransformFile),
                new SlimFormField("Target file:", this.txtDestinationFile),
                new SlimFormField(
                    "Options:",
                    new Div(this.chkPreserveWhitespace),
                    new Div(this.chkVerbose)
                )
            );
        }
    }
}
