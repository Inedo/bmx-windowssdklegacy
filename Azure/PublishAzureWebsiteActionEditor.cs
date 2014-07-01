using Inedo.BuildMaster.Web.Controls.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Web.Controls;
using Inedo.BuildMaster;
using System.Web.UI.WebControls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Azure
{
    internal sealed class PublishAzureWebsiteActionEditor : ActionEditorBase
    {
        private SourceControlFileFolderPicker txtProjectPath;
        private ValidatingTextBox txtProjectPublishProfileName;
        private ValidatingTextBox txtProjectPublishProfileXml;
        private ValidatingTextBox txtProjectBuildConfiguration;
        private ValidatingTextBox txtVisualStudioVersion;
        private ValidatingTextBox txtAdditionalArguments;
        private ValidatingTextBox txtUserName;
        private PasswordTextBox txtPassword;
        private CheckBox chkUseCustomProfileXml;

        public override void BindToForm(ActionBase extension)
        {
            var action = (PublishAzureWebsiteAction)extension;
            this.txtProjectPath.Text = Util.Path2.Combine(action.OverriddenSourceDirectory, action.ProjectPath);
            this.txtProjectPublishProfileName.Text = action.ProjectPublishProfileName;
            this.txtProjectPublishProfileXml.Text = action.ProjectPublishProfileXml;
            this.txtProjectBuildConfiguration.Text = action.ProjectBuildConfiguration;
            this.txtVisualStudioVersion.Text = action.VisualStudioVersion;
            this.txtAdditionalArguments.Text = action.AdditionalArguments;
            this.txtUserName.Text = action.UserName;
            this.txtPassword.Text = action.Password;
            this.chkUseCustomProfileXml.Checked = !string.IsNullOrWhiteSpace(action.ProjectPublishProfileXml);
        }

        public override ActionBase CreateFromForm()
        {
            return new PublishAzureWebsiteAction()
            {
                OverriddenSourceDirectory = Util.Path2.GetDirectoryName(this.txtProjectPath.Text),
                ProjectPath = Util.Path2.GetFileName(this.txtProjectPath.Text),
                ProjectPublishProfileName = txtProjectPublishProfileName.Text,
                ProjectPublishProfileXml = txtProjectPublishProfileXml.Text,
                ProjectBuildConfiguration = txtProjectBuildConfiguration.Text,
                VisualStudioVersion = string.IsNullOrEmpty(txtVisualStudioVersion.Text) ? "12.0" : txtVisualStudioVersion.Text,
                AdditionalArguments = txtAdditionalArguments.Text,
                UserName = txtUserName.Text,
                Password = txtPassword.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.chkUseCustomProfileXml = new CheckBox() { Text = "Use custom publish settings..." };
            var ctlProjectPublishProfileXmlContainer = new Div() { ID = "ctlProjectPublishProfileXmlContainer" };

            this.txtProjectPath = new SourceControlFileFolderPicker();
            this.txtProjectPublishProfileName = new ValidatingTextBox();
            this.txtProjectPublishProfileXml = new ValidatingTextBox() { TextMode = TextBoxMode.MultiLine, Rows = 7 };
            ctlProjectPublishProfileXmlContainer.Controls.Add(new Div("Enter custom publish profile XML:"), this.txtProjectPublishProfileXml);

            this.txtProjectBuildConfiguration = new ValidatingTextBox() { Required = true };
            this.txtVisualStudioVersion = new ValidatingTextBox() { DefaultText = "12.0" };
            this.txtAdditionalArguments = new ValidatingTextBox();
            this.txtUserName = new ValidatingTextBox() { DefaultText = "Inherit credentials from extension configuration" };
            this.txtPassword = new PasswordTextBox();

            this.Controls.Add(
                new SlimFormField("Project/Solution file:", this.txtProjectPath),
                new SlimFormField("Publish profile:", new Div("Profile Name:"), this.txtProjectPublishProfileName, this.chkUseCustomProfileXml, ctlProjectPublishProfileXmlContainer),
                new SlimFormField("Build configuration:", this.txtProjectBuildConfiguration),
                new SlimFormField("Visual Studio version:", this.txtVisualStudioVersion)
                {
                    HelpText = "Visual Studio must be installed in order to publish directly from the command line. Choose " 
                    + "the version of Visual Studio that is installed on the selected server in order for Web Deploy to use the "
                    + "appropriate build targets for the installed version. The default is 12.0 (Visual Studio 2013)."
                },
                new SlimFormField("Credentials:", new Div(new Div("Username:"), this.txtUserName), new Div(new Div("Password:"), this.txtPassword)),
                new SlimFormField("Additional MSBuild arguments:", this.txtAdditionalArguments)
            );

            this.Controls.BindVisibility(chkUseCustomProfileXml, ctlProjectPublishProfileXmlContainer);
        }
    }
}
