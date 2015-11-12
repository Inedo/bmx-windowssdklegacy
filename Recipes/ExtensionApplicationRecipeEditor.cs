using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal sealed class ExtensionApplicationRecipeEditor : RecipeEditorBase
    {
        private ExtensionApplicationWizardSteps wizardSteps = new ExtensionApplicationWizardSteps();
        private ValidatingTextBox txtApplicationName;

        private int ProviderId
        {
            get { return (int)(this.ViewState["ProviderId"] ?? 0); }
            set { this.ViewState["ProviderId"] = value; }
        }
        private string SolutionPath
        {
            get { return (string)this.ViewState["SolutionPath"]; }
            set { this.ViewState["SolutionPath"] = value; }
        }
        private ProjectInfo Project
        {
            get { return (ProjectInfo)this.ViewState["Project"]; }
            set { this.ViewState["Project"] = value; }
        }

        public override string ExecuteRecipeButtonText
        {
            get { return "Create Application"; }
        }
        public override bool DisplayAsWizard
        {
            get { return true; }
        }

        public override RecipeWizardSteps GetWizardStepsControl()
        {
            return this.wizardSteps;
        }

        public override RecipeBase CreateFromForm()
        {
            return new ExtensionApplicationRecipe()
            {
                ApplicationName = this.txtApplicationName.Text,
                ScmProviderId = this.ProviderId,
                SolutionPath = this.SolutionPath,
                Project = this.Project
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtApplicationName = new ValidatingTextBox
            {
                Required = true,
                Width = 300,
                ValidationExpression = "[0-9a-zA-Z]+"
            };
            try
            {
                txtApplicationName.Text = (Environment.UserDomainName ?? "").ToLowerInvariant() + "Extension";
                if (!string.IsNullOrEmpty(txtApplicationName.Text))
                    txtApplicationName.Text = txtApplicationName.Text[0].ToString().ToUpperInvariant() + txtApplicationName.Text.Substring(1);
            }
            catch {}

            this.txtApplicationName.ServerValidate +=
                (s, e) =>
                {
                    var applications = StoredProcs.Applications_GetApplications(null).Execute();
                    if (applications.Any(app => app.Application_Name.Equals(this.txtApplicationName.Text, StringComparison.OrdinalIgnoreCase)))
                        e.IsValid = false;
                };

            bool hasProviders = StoredProcs.Providers_GetProviders(Domains.ProviderTypes.SourceControl).Execute().Any();
            var ddlProvider = new ActionProviderPicker
            {
                AllowNameEntry = false,
                ProviderTypeCode = Domains.ProviderTypes.SourceControl,
                Visible = hasProviders
            };

            var ctlNoProviders = new InfoBox
            {
                BoxType = InfoBox.InfoBoxTypes.Error,
                Controls = { new LiteralControl("There are no source control providers set up in BuildMaster. Visit the <a href=\"/Administration/Providers/Overview.aspx?providerTypeCode=S\">Source Control Providers page</a> to add one.") },
                Visible = !hasProviders
            };

            var ctlMoreThanOneProject = new InfoBox
            {
                BoxType = InfoBox.InfoBoxTypes.Error,
                Controls = { new LiteralControl("There was more than one project in this solution. This application recipe only supports single-project solutions. Please go back to the previous step and select an extension solution with only one project.") },
                Visible = false
            };

            var ctlOneProject = new InfoBox
            {
                BoxType = InfoBox.InfoBoxTypes.Success,
                Controls = { new LiteralControl("There solution contains a single project. You may advance to the summary step.") }
            };

            var ctlSolutionPath = new SourceControlFileFolderPicker
            {
                DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles,
                BindToActionSourceControlProvider = true,
                Width = 300
            };

            var ffSolutionPath = new StandardFormField("Path of solution file (.sln):", ctlSolutionPath);

            this.wizardSteps.SelectOrganizationName.Controls.Add(
                new FormFieldGroup(
                    "Application Name",
                    "For custom extensions, the name should be <em>InitechExtension</em>, where \"Initech\" is your company's name. There's no need to have more than one custom extension per organization<br /><br />" 
                    + "For cloning BuildMaster extensions, use the exact name of the extension, for example <em>WindowsSdk</em><br /><br />.",
                    false,
                    new StandardFormField(
                        "Application Name (alpha-numeric only):",
                        txtApplicationName
                    )
                )
            );

            txtApplicationName.Load += (s, e) => this.wizardSteps.DownloadInstructions.ApplicationName = txtApplicationName.Text;

            this.wizardSteps.SelectProviderAndSolution.Controls.Add(
                new FormFieldGroup(
                    "Source Control",
                    "Select the Source Control Provider and the path to the extension's solution file.",
                    true,
                    new StandardFormField(
                        "Source Control Provider:",
                        ddlProvider,
                        ctlNoProviders,
                        new Div(
                            new ActionServerPicker { ID = "bm-action-server-id" }
                        ) { Style = "display: none;" }
                    ),
                    ffSolutionPath
                )
            );
            this.WizardStepChange +=
                (s, e) =>
                {
                    if (e.CurrentStep != this.wizardSteps.SelectProviderAndSolution)
                        return;

                    this.ProviderId = (int)ddlProvider.ProviderId;

                    using (var proxy = Util.Proxy.CreateProviderProxy(this.ProviderId))
                    {
                        var scm = proxy.TryGetService<SourceControlProviderBase>();
                        byte[] fileBytes = scm.GetFileContents(ctlSolutionPath.Text);

                        if (ctlSolutionPath.Text.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                        {
                            var solution = Solution.Load(new MemoryStream(fileBytes));

                            if (solution.Projects.Count > 1)
                            {
                                ctlMoreThanOneProject.Visible = true;
                                ctlOneProject.Visible = false;
                            }

                            this.SolutionPath = new ProjectInfo(scm.DirectorySeparator, ctlSolutionPath.Text).ScmDirectoryName;
                            this.Project = new ProjectInfo(scm.DirectorySeparator, new ProjectInfo(scm.DirectorySeparator, ctlSolutionPath.Text).ProjectFileName);
                        }
                    }
                };

            this.wizardSteps.OneProjectVerification.Controls.Add(
                new FormFieldGroup(
                    "One Project Verification",
                    "This step ensures that there is only one project in the selected solution.",
                    true,
                    new StandardFormField(
                        "",
                        ctlMoreThanOneProject,
                        ctlOneProject
                    )
                )
            );

            this.wizardSteps.Confirmation.Controls.Add(
                new FormFieldGroup(
                    "Summary",
                    "This is a summary of the details of the application that will be created. The application's deployment plan may be modified after creation if necessary.",
                    true,
                    new StandardFormField(
                        "",
                        new Summary(this)
                    )
                )
            );
        }

        private sealed class Summary : Control
        {
            private ExtensionApplicationRecipeEditor editor;

            public Summary(ExtensionApplicationRecipeEditor editor)
            {
                this.editor = editor;
            }

            protected override void Render(HtmlTextWriter writer)
            {
                if (editor.ProviderId == 0 || editor.SolutionPath == null || string.IsNullOrEmpty(editor.txtApplicationName.Text))
                    return;

                writer.Write(
                    "<p><strong>Source Control Provider: </strong> {0}</p>" +
                    "<p><strong>Solution Path: </strong> {1}</p>" +
                    "<p><strong>Application Name: </strong> {2}</p>",
                    StoredProcs.Providers_GetProvider(editor.ProviderId).ExecuteDataRow()[TableDefs.Providers.Provider_Name],
                    editor.SolutionPath,
                    editor.txtApplicationName.Text
                );
            }
        }
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
