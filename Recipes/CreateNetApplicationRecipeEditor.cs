using System;
using System.Collections.Generic;
using System.IO;
using System.Web.UI;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using System.Linq;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    internal sealed class CreateNetApplicationRecipeEditor : RecipeEditorBase
    {
        private CreateNetApplicationWizardSteps wizardSteps = new CreateNetApplicationWizardSteps();

        public CreateNetApplicationRecipeEditor()
        {
        }

        public override RecipeBase CreateFromForm()
        {
            return new CreateNetApplicationRecipe
            {
                SolutionPath = this.SolutionPath,
                Projects = this.Projects,
                ScmProviderId = this.ProviderId
            };
        }

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
        private ProjectInfo[] Projects
        {
            get { return (ProjectInfo[])this.ViewState["Projects"] ?? new ProjectInfo[0]; }
            set { this.ViewState["Projects"] = value; }
        }

        protected override void CreateChildControls()
        {
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

            var ctlProjectPath = new SourceControlFileFolderPicker { DisplayMode = SourceControlBrowser.DisplayModes.FoldersAndFiles };

            var ffProjectPath = new StandardFormField("Path of solution or project:", ctlProjectPath) { Visible = false };

            ddlProvider.SelectedIndexChanged +=
                (s, e) =>
                {
                    ctlProjectPath.SourceControlProviderId = int.Parse(ddlProvider.SelectedValue);
                    this.ProviderId = ctlProjectPath.SourceControlProviderId ?? 0;
                    ffProjectPath.Visible = ctlProjectPath.SourceControlProviderId > 0;
                };

            var ctlProjectsInSolution = new CheckBoxList();

            var ctlOneProject = new InfoBox()
            {
                BoxType = InfoBox.InfoBoxTypes.Success,
                Controls = { new LiteralControl("You have selected a project file, you may click Next to select a configuration file.") },
                Visible = false
            };

            var ctlConfigFiles = new CheckBoxList();

            var ctlNoConfigFiles = new InfoBox()
            {
                BoxType = InfoBox.InfoBoxTypes.Warning,
                Controls = { new LiteralControl("There are no files with .config extension in any of the selected projects, therefore configuration files for this project/solution will have to be created manually.") },
                Visible = false
            };

            var ffgTargets = new FormFieldGroup(
                "Deployment Targets",
                "Select the location to deploy project outputs.",
                true
            );

            this.Load +=
                (s, e) =>
                {
                    for(int i = 0; i < this.Projects.Length; i++)
                    {
                        ffgTargets.FormFields.Add(
                            new StandardFormField(
                                this.Projects[i].Name + ":",
                                new SourceControlFileFolderPicker
                                {
                                    ID = "ctlDeployTarget" + i,
                                    DisplayMode = SourceControlBrowser.DisplayModes.Folders
                                }
                            )
                        );
                    }
                };

            this.wizardSteps.SelectProviderAndFile.Controls.Add(
                new FormFieldGroup(
                    "Source Control",
                    "Select the Source Control Provider and the path of a project or solution.",
                    true,
                    new StandardFormField(
                        "Source Control Provider:",
                        ddlProvider,
                        ctlNoProviders
                    ),
                    ffProjectPath
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectProviderAndFile)
                    return;

                using (var proxy = Util.Proxy.CreateProviderProxy(ctlProjectPath.SourceControlProviderId ?? 0))
                {
                    var scm = proxy.TryGetService<SourceControlProviderBase>();
                    var fileBytes = (byte[])scm.GetFileContents(ctlProjectPath.Text);

                    if (ctlProjectPath.Text.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                    {
                        var solution = Solution.Load(new MemoryStream(fileBytes, false));
                        ctlProjectsInSolution.Items.AddRange(
                            solution.Projects
                                .OrderBy(p => p.Name)
                                .Select(p => new ListItem(p.Name, p.ProjectPath))
                        );

                        this.SolutionPath = new ProjectInfo(scm.DirectorySeparator, ctlProjectPath.Text).ScmDirectoryName;
                    }
                    else
                    {
                        this.SolutionPath = new ProjectInfo(scm.DirectorySeparator, ctlProjectPath.Text).ScmDirectoryName;
                        this.Projects = new[] { new ProjectInfo(scm.DirectorySeparator, new ProjectInfo(scm.DirectorySeparator, ctlProjectPath.Text).ProjectFileName) };
                        ctlOneProject.Visible = true;
                    }
                }
            };

            this.wizardSteps.SelectProjectsInSolution.Controls.Add(
                new FormFieldGroup(
                    "Projects",
                    "Select the projects in the solution that you would like to create deployables for.",
                    true,
                    new StandardFormField(
                        "",
                        ctlProjectsInSolution,
                        ctlOneProject
                    )
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectProjectsInSolution)
                    return;
                using(var scm = Util.Providers.CreateProviderFromId<SourceControlProviderBase>(this.ProviderId))
                {
                    var dirSeparator = scm.DirectorySeparator;

                    if (ctlProjectPath.Text.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
                    {
                        this.Projects = ctlProjectsInSolution.Items
                            .Cast<ListItem>()
                            .Where(i => i.Selected)
                            .Select(i => new ProjectInfo(dirSeparator, i.Value.Replace('\\', dirSeparator)))
                            .ToArray();
                    }

                    ParseProjects(ctlConfigFiles, ctlNoConfigFiles, scm);
                }
            };

            this.wizardSteps.SelectConfigFiles.Controls.Add(
                new FormFieldGroup(
                    "Configuration Files",
                    "Select any configuration files that you would like BuildMaster to manage.",
                    true,
                    new StandardFormField(
                        "",
                        ctlConfigFiles,
                        ctlNoConfigFiles
                    )
                )
            );
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectConfigFiles)
                    return;
                var dict = this.Projects.ToDictionary(p => p.ScmPath);
                var configFiles = ctlConfigFiles.Items
                    .Cast<ListItem>()
                    .Where(i => i.Selected)
                    .Select(i => new { Path = i.Value.Split('|')[0], Name = i.Value.Split('|')[1] })
                    .GroupBy(c => c.Path);

                foreach (var cfg in configFiles)
                    dict[cfg.Key].ConfigFiles.AddRange(cfg.Select(c => c.Name));
            };
            
            this.wizardSteps.SelectDeploymentPaths.Controls.Add(ffgTargets);
            this.WizardStepChange += (s, e) =>
            {
                if (e.CurrentStep != this.wizardSteps.SelectDeploymentPaths)
                    return;
                var controls = this.wizardSteps.SelectDeploymentPaths.Controls
                            .Find<SourceControlFileFolderPicker>()
                            .Where(c => c.ID.StartsWith("ctlDeployTarget"));

                foreach (var control in controls)
                    this.Projects[int.Parse(control.ID.Substring("ctlDeployTarget".Length))].DeploymentTarget = control.Text;
            };

            this.wizardSteps.Confirmation.Controls.Add(
                new FormFieldGroup(
                    "Overview",
                    "Confirm that the actions listed here are correct, then click the Execute Recipe button.",
                    true,
                    new StandardFormField(
                        "",
                        new RecipeOverview(this)
                    )
                )
            );

            base.CreateChildControls();
        }

        private void ParseProjects(CheckBoxList ctlConfigFiles, InfoBox ctlNoConfigFiles, SourceControlProviderBase provider)
        {
            var parsedProjects = this.Projects
                .Select(p => ReadProject(provider, this.SolutionPath + provider.DirectorySeparator + p.ScmPath))
                .ToArray();

            if (parsedProjects.SelectMany(p => GetConfigFiles(p)).Any())
            {
                for (int i = 0; i < this.Projects.Length; i++)
                {
                    foreach (var configFile in GetConfigFiles(parsedProjects[i]))
                        ctlConfigFiles.Items.Add(new ListItem(configFile + " in " + this.Projects[i].Name, this.Projects[i].ScmPath + '|' + configFile));
                }
            }
            else
            {
                ctlNoConfigFiles.Visible = true;
            }
        }

        private static IEnumerable<string> GetConfigFiles(MSBuildProject project)
        {
            return project.Files
                .Where(p => !string.Equals(p.Name, "packages.config", StringComparison.OrdinalIgnoreCase))
                .Where(p => p.Name.EndsWith(".config", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name);
        }

        private static MSBuildProject ReadProject(SourceControlProviderBase provider, string path)
        {
            var fileBytes = provider.GetFileContents(path);
            return MSBuildProject.Load(new MemoryStream(fileBytes));
        }

        private static string GetProjectPath(SourceControlProviderBase provider, string solutionPath, string relativeProjectPath)
        {
            var separator = provider.DirectorySeparator;
            var solutionDir = solutionPath.Substring(0, solutionPath.LastIndexOf(separator));
            return solutionDir + separator + relativeProjectPath.Replace('\\', separator).TrimStart(separator);
        }

        private sealed class RecipeOverview : Control
        {
            private CreateNetApplicationRecipeEditor form;

            public RecipeOverview(CreateNetApplicationRecipeEditor form)
            {
                this.form = form;
            }

            protected override void Render(HtmlTextWriter writer)
            {
                writer.Write("<p>Projects to be captured as outputs:</p>");
                writer.Write("<ul>");
                foreach (var project in this.form.Projects)
                {
                    writer.Write("<li>");
                    writer.WriteEncodedText(project.Name);
                    if (project.ConfigFiles.Any())
                    {
                        writer.Write("<ul>");
                        writer.Write("<li>Configuration Files:</li>");
                        writer.Write("<ul>");
                        foreach (var configFile in project.ConfigFiles)
                        {
                            writer.Write("<li>");
                            writer.WriteEncodedText(configFile);
                            writer.Write("</li>");
                        }

                        writer.Write("</ul>");
                        writer.Write("</ul>");
                    }

                    writer.Write("<ul>");
                    writer.Write("<li>Deploy to Path:</li>");
                    writer.Write("<ul>");
                    writer.Write("<li>");
                    writer.WriteEncodedText(project.DeploymentTarget);
                    writer.Write("</li>");
                    writer.Write("</ul>");
                    writer.Write("</ul>");

                    writer.Write("</li>");
                }

                writer.Write("</ul>");
            }
        }

        public override RecipeWizardSteps GetWizardStepsControl()
        {
            return this.wizardSteps;
        }

        public override bool DisplayAsWizard { get { return true; } }
    }
}
