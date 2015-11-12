using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.IO;
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
            var action = (SignExeAction)extension;
            this.txtSubject.Text = action.SubjectName;
            this.txtTimestampServer.Text = action.TimestampServer;
            this.txtContentDescription.Text = action.ContentDescription;
            this.txtContentUrl.Text = action.ContentUrl;
            if (string.IsNullOrEmpty(action.OverriddenSourceDirectory))
                this.ctlSignExe.Text = action.SignExePath;
            else
                this.ctlSignExe.Text = PathEx.Combine(action.OverriddenSourceDirectory, action.SignExePath);
        }
        public override ActionBase CreateFromForm()
        {
            return new SignExeAction
            {
                SubjectName = this.txtSubject.Text,
                TimestampServer = this.txtTimestampServer.Text,
                ContentDescription = this.txtContentDescription.Text,
                ContentUrl = this.txtContentUrl.Text,
                SignExePath = PathEx.GetFileName(this.ctlSignExe.Text),
                OverriddenSourceDirectory = PathEx.GetDirectoryName(this.ctlSignExe.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.txtSubject = new ValidatingTextBox { Required = true };
            this.txtTimestampServer = new ValidatingTextBox { DefaultText = "(none)" };
            this.txtContentDescription = new ValidatingTextBox { DefaultText = "(none)", };
            this.txtContentUrl = new ValidatingTextBox { DefaultText = "(none)" };
            this.ctlSignExe = new SourceControlFileFolderPicker { DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles };

            this.Controls.Add(
                new SlimFormField("File to sign:", this.ctlSignExe),
                new SlimFormField("Certificate subject:", this.txtSubject),
                new SlimFormField("Timestamp server URL:", this.txtTimestampServer),
                new SlimFormField("Description:", this.txtContentDescription),
                new SlimFormField("Information URL:", this.txtContentUrl)
            );
        }
    }
}
