using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using FileAssociation = Inedo.BuildMasterExtensions.WindowsSdk.DotNet.ClickOnceAction.FileAssociation;

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
        private ValidatingTextBox txtMinVersion;
        private ValidatingTextBox txtEntryPointFile;
        private CheckBox chkMapFileExtensions;
        private CheckBox chkInstallApplication;
        private ValidatingTextBox txtIconFile;
        private ValidatingTextBox txtFilesExcludedFromManifest;
        private ValidatingTextBox txtAppCodeBaseDirectory;
        private CheckBox chkCreateDesktopIcon;
        private CheckBox chkStartupCheckForUpdate;
        private CheckBox chkTrustUrlParameters;

        private ValidatingTextBox txtFileAssociationExtension1;
        private ValidatingTextBox txtFileAssociationDescription1;
        private ValidatingTextBox txtFileAssociationProgId1;
        private ValidatingTextBox txtFileAssociationDefaultIcon1;

        private ValidatingTextBox txtFileAssociationExtension2;
        private ValidatingTextBox txtFileAssociationDescription2;
        private ValidatingTextBox txtFileAssociationProgId2;
        private ValidatingTextBox txtFileAssociationDefaultIcon2;

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        protected override void CreateChildControls()
        {
            this.txtApplicationName = new ValidatingTextBox { Required = true };
            this.txtProviderUrl = new ValidatingTextBox { Required = true };
            this.txtCertificatePath = new SourceControlFileFolderPicker();
            this.txtCertificatePassword = new PasswordTextBox();
            this.txtCertificateHash = new ValidatingTextBox();
            this.txtVersion = new ValidatingTextBox { Required = true };
            this.txtMinVersion = new ValidatingTextBox();
            this.chkMapFileExtensions = new CheckBox { Text = "Rename files to .deploy" };
            this.chkInstallApplication = new CheckBox { Text = "Install application onto local machine" };
            this.chkCreateDesktopIcon = new CheckBox { Text = "Create desktop icon" };
            this.chkStartupCheckForUpdate = new CheckBox { Text = "Check for update at startup" };
            this.txtEntryPointFile = new ValidatingTextBox();
            this.txtFilesExcludedFromManifest = new ValidatingTextBox { TextMode = TextBoxMode.MultiLine };
            this.txtIconFile = new ValidatingTextBox();
            this.txtAppCodeBaseDirectory = new ValidatingTextBox();
            this.chkTrustUrlParameters = new CheckBox { Text = "Trust URL parameters" };

            this.txtFileAssociationDefaultIcon1 = new ValidatingTextBox();
            this.txtFileAssociationDescription1 = new ValidatingTextBox();
            this.txtFileAssociationExtension1 = new ValidatingTextBox();
            this.txtFileAssociationProgId1 = new ValidatingTextBox();

            this.txtFileAssociationDefaultIcon2 = new ValidatingTextBox();
            this.txtFileAssociationDescription2 = new ValidatingTextBox();
            this.txtFileAssociationExtension2 = new ValidatingTextBox();
            this.txtFileAssociationProgId2 = new ValidatingTextBox();

            this.Controls.Add(
                new FormFieldGroup(
                    "Application Settings",
                    "Configuration for the application. Note that the version number and minimum version number "
                    + "must be of the form 0.0.0.0. The minimum version can be used to force an update preventing the user from skipping it."
                    + " The provider URL should be where the application is deployed to (e.g. http://example.com/MyApp/)",
                    false,
                    new StandardFormField(
                        "Application Name:",
                        this.txtApplicationName),
                    new StandardFormField(
                        "Version Number:",
                        this.txtVersion),
                        new StandardFormField(
                        "Minimum Version Number:",
                        this.txtMinVersion),
                    new StandardFormField(
                        "Provider URL:",
                        this.txtProviderUrl),
                    new StandardFormField("Icon File:", this.txtIconFile),
                    new StandardFormField("Application Code Base Directory:", this.txtAppCodeBaseDirectory),
                    new StandardFormField(
                        "Entry Point File:",
                        this.txtEntryPointFile),
                    new StandardFormField("Files to exclude from manifest:",
                        this.txtFilesExcludedFromManifest),
                    new StandardFormField(String.Empty, this.chkTrustUrlParameters),
                    new StandardFormField("File Association 1:",
                        new StandardFormField("Default Icon:", this.txtFileAssociationDefaultIcon1),
                        new StandardFormField("Description:", this.txtFileAssociationDescription1),
                        new StandardFormField("Extension:", this.txtFileAssociationExtension1),
                        new StandardFormField("Prog Id:", this.txtFileAssociationProgId1)),
                    new StandardFormField("File Association 2:",
                        new StandardFormField("Default Icon:", this.txtFileAssociationDefaultIcon2),
                        new StandardFormField("Description:", this.txtFileAssociationDescription2),
                        new StandardFormField("Extension:", this.txtFileAssociationExtension2),
                        new StandardFormField("Prog Id:", this.txtFileAssociationProgId2))
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
                    "Installation Settings",
                    "Indicates whether or not the ClickOnce application should install onto the local machine, "
                    + "or whether it should run from the Web. Installing an application gives that application a "
                    + "presence in the Windows Start menu.",
                    false,
                    new StandardFormField(string.Empty, this.chkInstallApplication),
                    new StandardFormField(string.Empty, this.chkCreateDesktopIcon),
                    new StandardFormField(string.Empty, this.chkStartupCheckForUpdate)),
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
            this.txtIconFile.Text = c1action.IconFile;
            this.chkCreateDesktopIcon.Checked = c1action.CreateDesktopIcon;
            this.chkStartupCheckForUpdate.Checked = c1action.StartupUpdateCheck;
            this.txtMinVersion.Text = c1action.MinVersion;
            this.txtEntryPointFile.Text = c1action.EntryPointFile;
            this.txtFilesExcludedFromManifest.Text = String.Join(Environment.NewLine, c1action.FilesExcludedFromManifest);
            this.txtAppCodeBaseDirectory.Text = c1action.AppCodeBaseDirectory;
            this.chkTrustUrlParameters.Checked = c1action.TrustUrlParameters;

            if (c1action.FileAssociations.Length >= 1)
            {
                this.txtFileAssociationDefaultIcon1.Text = c1action.FileAssociations[0].DefaultIcon;
                this.txtFileAssociationDescription1.Text = c1action.FileAssociations[0].Description;
                this.txtFileAssociationExtension1.Text = c1action.FileAssociations[0].Extension;
                this.txtFileAssociationProgId1.Text = c1action.FileAssociations[0].ProgId;
            }

            if (c1action.FileAssociations.Length >= 2)
            {
                this.txtFileAssociationDefaultIcon2.Text = c1action.FileAssociations[1].DefaultIcon;
                this.txtFileAssociationDescription2.Text = c1action.FileAssociations[1].Description;
                this.txtFileAssociationExtension2.Text = c1action.FileAssociations[1].Extension;
                this.txtFileAssociationProgId2.Text = c1action.FileAssociations[1].ProgId;
            }
        }

        public override ActionBase CreateFromForm()
        {
            var fileAssociations = new List<FileAssociation>();

            if (!String.IsNullOrWhiteSpace(this.txtFileAssociationDefaultIcon1.Text)
                && !String.IsNullOrWhiteSpace(this.txtFileAssociationDescription1.Text)
                && !String.IsNullOrWhiteSpace(this.txtFileAssociationExtension1.Text)
                && !String.IsNullOrWhiteSpace(this.txtFileAssociationProgId1.Text))
            {
                fileAssociations.Add(new FileAssociation
                {
                    DefaultIcon = this.txtFileAssociationDefaultIcon1.Text,
                    Description = this.txtFileAssociationDescription1.Text,
                    Extension = this.txtFileAssociationExtension1.Text,
                    ProgId = this.txtFileAssociationProgId1.Text
                });
            }

            if (!String.IsNullOrWhiteSpace(this.txtFileAssociationDefaultIcon2.Text)
                && !String.IsNullOrWhiteSpace(this.txtFileAssociationDescription2.Text)
                && !String.IsNullOrWhiteSpace(this.txtFileAssociationExtension2.Text)
                && !String.IsNullOrWhiteSpace(this.txtFileAssociationProgId2.Text))
            {
                fileAssociations.Add(new FileAssociation
                {
                    DefaultIcon = this.txtFileAssociationDefaultIcon2.Text,
                    Description = this.txtFileAssociationDescription2.Text,
                    Extension = this.txtFileAssociationExtension2.Text,
                    ProgId = this.txtFileAssociationProgId2.Text
                });
            }

            return new ClickOnceAction
            {
                ApplicationName = this.txtApplicationName.Text,
                ProviderUrl = this.txtProviderUrl.Text,
                CertificatePath = this.txtCertificatePath.Text,
                CertificateHash = this.txtCertificateHash.Text,
                CertificatePassword = this.txtCertificatePassword.Text,
                Version = this.txtVersion.Text,
                MapFileExtensions = this.chkMapFileExtensions.Checked,
                InstallApplication = this.chkInstallApplication.Checked,
                IconFile = this.txtIconFile.Text,
                CreateDesktopIcon = this.chkCreateDesktopIcon.Checked,
                StartupUpdateCheck = this.chkStartupCheckForUpdate.Checked,
                MinVersion = this.txtMinVersion.Text,
                EntryPointFile = this.txtEntryPointFile.Text,
                FilesExcludedFromManifest = this.txtFilesExcludedFromManifest.Text.Split('\n').Select(x => x.Trim()).ToArray(),
                AppCodeBaseDirectory = this.txtAppCodeBaseDirectory.Text,
                TrustUrlParameters = this.chkTrustUrlParameters.Checked,
                FileAssociations = fileAssociations.ToArray()
            };
        }
    }
}
