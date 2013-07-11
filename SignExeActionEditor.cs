using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    internal sealed class SignExeActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtSubject;
        private ValidatingTextBox txtTimestampServer;
        private ValidatingTextBox txtContentDescription;
        private ValidatingTextBox txtContentUrl;
        private SourceControlFileFolderPicker ctlSignExe;

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var action = (SignExeAction)extension;
            this.txtSubject.Text = action.SubjectName;
            this.txtTimestampServer.Text = action.TimestampServer;
            this.txtContentDescription.Text = action.ContentDescription;
            this.txtContentUrl.Text = action.ContentUrl;
            if (string.IsNullOrEmpty(action.OverriddenSourceDirectory))
                this.ctlSignExe.Text = action.SignExePath;
            else
                this.ctlSignExe.Text = Util.Path2.Combine(action.OverriddenSourceDirectory, action.SignExePath);
        }
        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new SignExeAction
            {
                SubjectName = this.txtSubject.Text,
                TimestampServer = this.txtTimestampServer.Text,
                ContentDescription = this.txtContentDescription.Text,
                ContentUrl = this.txtContentUrl.Text,
                SignExePath = Util.Path2.GetFileName(this.ctlSignExe.Text),
                OverriddenSourceDirectory = Util.Path2.GetDirectoryName(this.ctlSignExe.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtSubject = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtTimestampServer = new ValidatingTextBox { DefaultText = "(none)", Width = 300 };
            this.txtContentDescription = new ValidatingTextBox { DefaultText = "(none)", Width = 300 };
            this.txtContentUrl = new ValidatingTextBox { DefaultText = "(none)", Width = 300 };
            this.ctlSignExe = new SourceControlFileFolderPicker
            {
                ServerId = this.ServerId,
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Target",
                    "Specify the executable file to sign.",
                    false,
                    new StandardFormField("File to Sign:", this.ctlSignExe)
                ),
                new FormFieldGroup(
                    "Subject",
                    "The subject name (or a substring of the subject name) of the signing certificate.",
                    false,
                    new StandardFormField("Subject Name:", this.txtSubject)
                ),
                new FormFieldGroup(
                    "Timestamp Server URL",
                    "The URL of the timestamp server used when signing the executable. If not specified, no timestamp is used.",
                    false,
                    new StandardFormField("Timestamp Server URL:", this.txtTimestampServer)
                ),
                new FormFieldGroup(
                    "Additional Metadata",
                    "Optionally provide additional information to add to the signature.",
                    true,
                    new StandardFormField("Description of Signed Content:", this.txtContentDescription),
                    new StandardFormField("URL with More Information:", this.txtContentUrl)
                )
            );
        }
    }
}
