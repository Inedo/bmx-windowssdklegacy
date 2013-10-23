using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class ClickOnceActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtApplicationName;
        private ValidatingTextBox txtProviderUrl;
        private SourceControlFileFolderPicker txtCertificatePath;
        private PasswordTextBox txtCertificatePassword;
        private ValidatingTextBox txtCertificateHash;
        private ValidatingTextBox txtVersion;
        private CheckBox chkMapFileExtensions;
        private CheckBox chkInstallApplication;

        public override bool DisplaySourceDirectory { get { return true; } }

        protected override void CreateChildControls()
        {
            this.txtApplicationName = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtProviderUrl = new ValidatingTextBox { Required = true, Width = 300 };
            this.txtCertificatePath = new SourceControlFileFolderPicker { ServerId = this.ServerId };
            this.txtCertificatePassword = new PasswordTextBox { Width = 250 };
            this.txtCertificateHash = new ValidatingTextBox { Width = 300 };
            this.txtVersion = new ValidatingTextBox { Required = true, Width = 300 };
            this.chkMapFileExtensions = new CheckBox { Text = "Rename files to .deploy" };
            this.chkInstallApplication = new CheckBox { Text = "Install application onto local machine" };

            this.Controls.Add(
                new FormFieldGroup(
                    "Application Settings",
                    "Configuration for the application. Note that the version number "
                    + "must be of the form 0.0.0.0 and the provider URL should be where "
                    + "the application is deployed to (e.g. http://example.com/MyApp/)",
                    false,
                    new StandardFormField(
                        "Application Name:",
                        this.txtApplicationName),
                    new StandardFormField(
                        "Version Number:",
                        this.txtVersion),
                    new StandardFormField(
                        "Provider URL:",
                        this.txtProviderUrl)
                    ),
                new FormFieldGroup(
                    "File Extension Mapping",
                    "Determines whether files in the deployment will have a .deploy extension. "
                    + "ClickOnce will strip this extension off these files as soon as it downloads them "
                    + "from the Web server. This parameter allows all the files within a ClickOnce deployment "
                    + "to be downloaded from a Web server that blocks transmission of files ending in \"unsafe\" "
                    + "extensions such as .exe. ",
                    false,
                    new StandardFormField(string.Empty, this.chkMapFileExtensions)),
                new FormFieldGroup(
                    "Install Application",
                    "Indicates whether or not the ClickOnce application should install onto the local machine, "
                    + "or whether it should run from the Web. Installing an application gives that application a " 
                    + "presence in the Windows Start menu.",
                    false,
                    new StandardFormField(string.Empty, this.chkInstallApplication)),
                new FormFieldGroup(
                    "Certificate Settings",
                    "ClickOnce applications must be signed with an X509 certificate, "
                    + "which may be stored on disk or in the local cert store. "
                    + "<br /><br />Note that either a Certificate Path or Certificate Hash "
                    + "must be selected, but not both",
                    true,
                    new StandardFormField(
                        "Certificate Path:",
                        this.txtCertificatePath),
                    new StandardFormField(
                        "Certificate Hash:",
                        this.txtCertificateHash),
                    new StandardFormField(
                        "Certificate Password:",
                        this.txtCertificatePassword)));
        }

        public override void BindToForm(ActionBase extension)
        {
            var c1action = (ClickOnceAction)extension;

            this.txtApplicationName.Text = c1action.ApplicationName;
            this.txtProviderUrl.Text = c1action.ProviderUrl;
            this.txtCertificatePath.Text = c1action.CertificatePath;
            this.txtCertificatePassword.Text = c1action.CertificatePassword;
            this.txtCertificateHash.Text = c1action.CertificateHash;
            this.txtVersion.Text = c1action.Version;
            this.chkMapFileExtensions.Checked = c1action.MapFileExtensions;
            this.chkInstallApplication.Checked = c1action.InstallApplication;
        }

        public override ActionBase CreateFromForm()
        {
            return new ClickOnceAction
            {
                ApplicationName = this.txtApplicationName.Text,
                ProviderUrl = this.txtProviderUrl.Text,
                CertificatePath = this.txtCertificatePath.Text,
                CertificateHash = this.txtCertificateHash.Text,
                CertificatePassword = this.txtCertificatePassword.Text,
                Version = this.txtVersion.Text,
                MapFileExtensions = this.chkMapFileExtensions.Checked,
                InstallApplication = this.chkInstallApplication.Checked
            };
        }
    }
}
