using System;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    internal sealed class ExtensionApplicationRecipeEditor : RecipeEditorBase
    {
        private ExtensionApplicationWizardSteps wizardSteps = new ExtensionApplicationWizardSteps();

        private ValidatingTextBox txtOrganizationName;

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

        public override string ExecuteRecipeButtonText { get { return "Create Application"; } }

        public override bool DisplayAsWizard { get { return true; } }

        public ExtensionApplicationRecipeEditor()
        {
        }

        public override RecipeWizardSteps GetWizardStepsControl()
        {
            return this.wizardSteps;
        }

        public override RecipeBase CreateFromForm()
        {
            return new ExtensionApplicationRecipe()
            {
                OrganizationName = this.txtOrganizationName.Text.Replace(" ", ""),
                ScmProviderId = this.ProviderId,
                SolutionPath = this.SolutionPath,
                Project = this.Project
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtOrganizationName = new ValidatingTextBox()
            {
                Required = true,
                Width = 300
            };
            this.txtOrganizationName.ServerValidate += (s, e) =>
            {
                var applications = StoredProcs.Applications_GetApplications(null).Execute();
                if (applications.Any(app => app.Application_Name.Equals(this.txtOrganizationName.Text.Replace(" ", "") + "Extension", StringComparison.OrdinalIgnoreCase)))
                    e.IsValid = false;
            };
           
            var ddlProvider = new DropDownList { AutoPostBack = true };
            ddlProvider.Items.Add(new ListItem("", "0"));
            var providerItems = StoredProcs.Providers_GetProviders(
                    Domains.ProviderTypes.SourceControl,
                    null,
                    null
                ).Execute()
                .Select(p => new ListItem(p.Provider_Name, p.Provider_Id.ToString()))
                .ToArray();
            ddlProvider.Items.AddRange(providerItems);
            ddlProvider.Visible = providerItems.Any();

            var ctlNoProviders = new InfoBox()
            {
                BoxType = InfoBox.InfoBoxTypes.Error,
                Controls = { new LiteralControl("There are no source control providers set up in BuildMaster. Visit the <a href=\"/Administration/Providers/Overview.aspx?providerTypeCode=S\">Source Control Providers page</a> to add one.") },
                Visible = !providerItems.Any()
            };

            var ctlMoreThanOneProject = new InfoBox()
            {
                BoxType = InfoBox.InfoBoxTypes.Error,
                Controls = { new LiteralControl("There was more than one project in this solution. This application recipe only supports single-project solutions. Please go back to the previous step and select an extension solution with only one project.") },
                Visible = false
            };
            var ctlOneProject = new InfoBox()
            {
                BoxType = InfoBox.InfoBoxTypes.Success,
                Controls = { new LiteralControl("There solution contains a single project. You may advance to the summary step.") }
            };

            var ctlSolutionPath = new SourceControlFileFolderPicker() { DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles };

            var ffSolutionPath = new StandardFormField("Path of solution:", ctlSolutionPath) { Visible = false };
            
            ddlProvider.SelectedIndexChanged += (s, e) =>
            {
                ctlSolutionPath.SourceControlProviderId = int.Parse(ddlProvider.SelectedValue);
                this.ProviderId = ctlSolutionPath.SourceControlProviderId ?? 0;
                ffSolutionPath.Visible = ctlSolutionPath.SourceControlProviderId > 0;
            };

            this.wizardSteps.SelectOrganizationName.Controls.Add(
                new FormFieldGroup(
                    "Organization Name",
                    "This should be your company or division name, e.g. \"Initech\". <br /><br />This is used to generate the sample code and also create the application name.",
                    true,
                    new StandardFormField(
                        "Organization Name:",
                        txtOrganizationName
                    )
                )
            );

            txtOrganizationName.Load += (S,E) => this.wizardSteps.DownloadInstructions.OrganizationName = txtOrganizationName.Text;

            this.wizardSteps.SelectProviderAndSolution.Controls.Add(
                new FormFieldGroup(
                    "Source Control",
                    "Select the Source Control Provider and the path to the extension's solution file.",
                    true,
                    new StandardFormField(
                        "Source Control Provider:",
                        ddlProvider,
                        ctlNoProviders
                    ),
                    ffSolutionPath
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectProviderAndSolution)
                    return;
                using (var scm = Util.Providers.CreateProviderFromId<SourceControlProviderBase>(ctlSolutionPath.SourceControlProviderId ?? 0))
                {
                    var fileBytes = scm.GetFileContents(ctlSolutionPath.Text);
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
                if (editor.ProviderId == 0 || editor.SolutionPath == null || string.IsNullOrEmpty(editor.txtOrganizationName.Text))
                    return;

                writer.Write(
                    "<p><strong>Source Control Provider: </strong> {0}</p>" +
                    "<p><strong>Solution Path: </strong> {1}</p>" +
                    "<p><strong>Application Name: </strong> {2}</p>",
                    StoredProcs.Providers_GetProvider(editor.ProviderId).ExecuteDataRow()[TableDefs.Providers.Provider_Name],
                    editor.SolutionPath,
                    editor.txtOrganizationName.Text.Replace(" ", "") + "Extension"
                );
            }
        }
    }
}
