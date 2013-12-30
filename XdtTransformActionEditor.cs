using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    /// <summary>
    /// Custom editor for the <see cref="XdtTransformAction"/> class.
    /// </summary>
    public sealed class XdtTransformActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtSourceFile;
        private ValidatingTextBox txtTransformFile;
        private ValidatingTextBox txtDestinationFile;
        private CheckBox chkPreserveWhitespace;
        private CheckBox chkVerbose;

        /// <summary>
        /// Gets a value indicating whether [display source directory].
        /// </summary>
        /// <value>
        /// <c>true</c> if [display source directory]; otherwise, <c>false</c>.
        /// </value>
        public override bool DisplaySourceDirectory { get { return true; } }

        /// <summary>
        /// Binds to form.
        /// </summary>
        /// <param name="extension">The extension.</param>
        public override void BindToForm(ActionBase extension)
        {
            var action = (XdtTransformAction)extension;

            this.txtSourceFile.Text = action.SourceFile;
            this.txtTransformFile.Text = action.TransformFile;
            this.txtDestinationFile.Text = Path.Combine(action.OverriddenTargetDirectory, action.DestinationFile);
            this.chkPreserveWhitespace.Checked = action.PreserveWhitespace;
            this.chkVerbose.Checked = action.Verbose;
        }

        /// <summary>
        /// Creates from form.
        /// </summary>
        /// <returns></returns>
        public override ActionBase CreateFromForm()
        {
            return new XdtTransformAction()
            {
                SourceFile = this.txtSourceFile.Text,
                TransformFile = this.txtTransformFile.Text,
                DestinationFile = Path.GetFileName(this.txtDestinationFile.Text),
                OverriddenTargetDirectory = Path.GetDirectoryName(this.txtDestinationFile.Text),
                PreserveWhitespace = this.chkPreserveWhitespace.Checked,
                Verbose = this.chkVerbose.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtSourceFile = new ValidatingTextBox() { Width = 300, Required = true, Text = "Web.config" };
            this.txtTransformFile = new ValidatingTextBox() { Width = 300, Required = true };
            this.txtDestinationFile = new ValidatingTextBox() { Width = 300, Required = true, Text = "Web.config" };
            this.chkPreserveWhitespace = new CheckBox() { Text = "Preserve Whitespace in Destination File", Checked = true };
            this.chkVerbose = new CheckBox() { Text = "Enable Verbose Logging", Checked = true };

            this.Controls.Add(
                new FormFieldGroup(
                    "Source File",
                    "The source file, relative to the source directory.",
                    false,
                    new StandardFormField("Source File:", this.txtSourceFile)
                ),
                new FormFieldGroup(
                    "Transform File",
                    "The transform file, relative to the source directory.",
                    false,
                    new StandardFormField("Transform File:", this.txtTransformFile)
                ),
                new FormFieldGroup(
                    "Destination File",
                    "The destination file, relative to the default directory.",
                    false,
                    new StandardFormField("Destination File:", this.txtDestinationFile)
                ),
                new FormFieldGroup(
                    "Additional Options",
                    "Specify whether whitespace should be preserved in the destination file, and if verbose logging should be captured.",
                    false,
                    new StandardFormField("", this.chkPreserveWhitespace),
                    new StandardFormField("", this.chkVerbose)
                )
            );
        }
    }
}
