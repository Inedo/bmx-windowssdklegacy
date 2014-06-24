using System;
using System.IO;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.ClientResources;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    internal sealed class BuildMSBuildProjectActionEditor : ActionEditorBase
    {
        private DropDownList ddlProjectBuildConfiguration;
        private HtmlGenericControl divConfig;
        private TextBox txtOtherConfig;
        private TextBox txtProjectPath;
        private CheckBox chkWebProject;
        private DropDownList ddlBuildOutputDir;
        private DropDownList ddlProjectBuildTargetPlatform;
        private TextBox txtOtherPlatform;
        private HtmlGenericControl divPlatform;
        private TextBox txtAdditionalProperties;
        private HtmlGenericControl divTargetDir;
        private SourceControlFileFolderPicker txtTargetDir;

        protected override void CreateChildControls()
        {
            this.ddlProjectBuildConfiguration = new DropDownList
            {
                ID = "ddlProjectBuildConfiguration",
                Items =
                {
                    new ListItem("Debug", "Debug"),
                    new ListItem("Release", "Release"),
                    new ListItem("Other", "Other")
                }
            };

            this.txtOtherConfig = new TextBox { Width = 150 };

            this.ddlProjectBuildTargetPlatform = new DropDownList
            {
                ID = "ddlProjectBuildTargetPlatform",
                Items =
                {
                    new ListItem("(Default)", string.Empty),
                    new ListItem("Any CPU", "AnyCPU"),
                    new ListItem("x86", "x86"),
                    new ListItem("x64", "x64"),
                    new ListItem("Other", "Other")
                }
            };

            this.txtOtherPlatform = new TextBox { Width = 150 };

            this.txtProjectPath = new TextBox { Width = 325 };

            this.chkWebProject = new CheckBox
            {
                ID = "chkWebProject",
                Text = "This is a Web Application project"
            };

            this.ddlBuildOutputDir = new DropDownList
            {
                ID = "ddlBuildOutputDir",
                Items =
                {
                    new ListItem("Specify output directory...", "target"),
                    new ListItem("Per project \\bin\\{config} directories", "bin"),
                }
            };

            this.txtTargetDir = new SourceControlFileFolderPicker
            {
                DefaultText = "default"
            };

            this.txtAdditionalProperties = new TextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            this.divConfig = new HtmlGenericControl("div") { ID = "divConfig" };
            this.divConfig.Style.Value = "display:none;";

            this.divConfig.Controls.Add(this.txtOtherConfig);

            this.divPlatform = new HtmlGenericControl("div") { ID = "divPlatform" };
            this.divPlatform.Style.Value = "display:none;";

            this.divPlatform.Controls.Add(this.txtOtherPlatform);

            this.divTargetDir = new HtmlGenericControl("div") { ID = "divTargetDir" };
            this.divTargetDir.Style.Value = "display:none;";

            this.divTargetDir.Controls.Add(this.txtTargetDir);

            this.Controls.Add(
                new SlimFormField("Project/solution file:", this.txtProjectPath, new Div(this.chkWebProject)),
                new SlimFormField("Configuration:", this.ddlProjectBuildConfiguration, this.divConfig),
                new SlimFormField("Platform:", this.ddlProjectBuildTargetPlatform, this.divPlatform),
                new SlimFormField("Output directory:", this.ddlBuildOutputDir, this.divTargetDir)
                {
                    HelpText = "The directory of the build output. The \\bin\\{config} option is recommended when building a solution file. If \"This is a Web Application project\" is selected, an output directory must be specified."
                },
                new SlimFormField("MSBuild properties:", this.txtAdditionalProperties)
                {
                    HelpText = HelpText.FromHtml("Additional properties, separated by newlines. Example:<br />WarningLevel=2<br />Optimize=false")
                },
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("BmExecuteMSBuildScriptActionEditor(");
                        InedoLib.Util.JavaScript.WriteJson(
                            w,
                            new
                            {
                                ddlConfigId = "#" + ddlProjectBuildConfiguration.ClientID,
                                divConfigId = "#" + divConfig.ClientID,
                                ddlPlatformId = "#" + ddlProjectBuildTargetPlatform.ClientID,
                                divPlatformId = "#" + divPlatform.ClientID,
                                ddlTargetDirId = "#" + ddlBuildOutputDir.ClientID,
                                divTargetDirId = "#" + divTargetDir.ClientID,
                                chkWebProjectId = "#" + chkWebProject.ClientID
                            }
                        );
                        w.Write(");");
                    }
                )
            );
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.IncludeClientResourceInPage(
                new JavascriptResource
                {
                    ResourcePath = "~/extension-resources/windowssdk/msbuild/executemsbuildscriptactioneditor.js?" + typeof(BuildMSBuildProjectActionEditor).Assembly.GetName().Version,
                    CompatibleVersions = { InedoLibCR.Versions.jq152, InedoLibCR.Versions.jq161, InedoLibCR.Versions.jq171 }
                }
            );

            base.OnPreRender(e);
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var buildAction = (BuildMSBuildProjectAction)extension;

            if (buildAction.ProjectBuildConfiguration == "Debug" || buildAction.ProjectBuildConfiguration == "Release")
            {
                this.ddlProjectBuildConfiguration.SelectedValue = buildAction.ProjectBuildConfiguration;
            }
            else
            {
                this.ddlProjectBuildConfiguration.SelectedValue = "Other";
                this.txtOtherConfig.Text = buildAction.ProjectBuildConfiguration;
            }

            var platform = buildAction.ProjectTargetPlatform ?? string.Empty;
            if (platform == string.Empty || platform == "AnyCPU" || platform == "x86" || platform == "x64")
            {
                this.ddlProjectBuildTargetPlatform.SelectedValue = platform;
            }
            else
            {
                this.ddlProjectBuildTargetPlatform.SelectedValue = "Other";
                this.txtOtherPlatform.Text = platform;
            }

            this.txtProjectPath.Text = Path.Combine(buildAction.OverriddenSourceDirectory ?? "", buildAction.ProjectPath);
            this.chkWebProject.Checked = buildAction.IsWebProject;
            this.txtAdditionalProperties.Text = buildAction.MSBuildProperties ?? "";
            if (buildAction.BuildToProjectConfigSubdirectories)
            {
                this.ddlBuildOutputDir.SelectedValue = "bin";
            }
            else
            {
                this.ddlBuildOutputDir.SelectedValue = "target";
                this.txtTargetDir.Text = buildAction.OverriddenTargetDirectory;
            }
        }

        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            var buildAction = new BuildMSBuildProjectAction();

            if (ddlProjectBuildConfiguration.SelectedValue != "Other")
                buildAction.ProjectBuildConfiguration = this.ddlProjectBuildConfiguration.SelectedValue;
            else
                buildAction.ProjectBuildConfiguration = this.txtOtherConfig.Text;

            if (ddlProjectBuildTargetPlatform.SelectedValue != "Other")
                buildAction.ProjectTargetPlatform = this.ddlProjectBuildTargetPlatform.SelectedValue;
            else
                buildAction.ProjectTargetPlatform = this.txtOtherPlatform.Text;

            if (SeparateOverriddenSourceDirectory(this.txtProjectPath.Text, this.txtTargetDir.Text))
            {
                buildAction.OverriddenSourceDirectory = Path.GetDirectoryName(this.txtProjectPath.Text);
                buildAction.ProjectPath = Path.GetFileName(this.txtProjectPath.Text);
            }
            else
            {
                buildAction.ProjectPath = this.txtProjectPath.Text;
            }

            buildAction.IsWebProject = this.chkWebProject.Checked;
            buildAction.MSBuildProperties = this.txtAdditionalProperties.Text;

            if (this.ddlBuildOutputDir.SelectedValue == "bin" && !this.chkWebProject.Checked)
            {
                buildAction.BuildToProjectConfigSubdirectories = true;
                buildAction.OverriddenTargetDirectory = "";
            }
            else
            {
                buildAction.BuildToProjectConfigSubdirectories = false;
                buildAction.OverriddenTargetDirectory = this.txtTargetDir.Text;
            }

            return buildAction;
        }

        private static bool SeparateOverriddenSourceDirectory(string projectPath, string targetDir)
        {
            if (!string.IsNullOrEmpty(targetDir))
                return true;
            if (projectPath.StartsWith("~") || projectPath.StartsWith("/") || projectPath.StartsWith("\\"))
                return true;
            if (!projectPath.Contains("/") && !projectPath.Contains("\\"))
                return true;

            /* At this point, the target dir is "default", and the project path is relative to the default 
              so we should use the pre-3.7 and NOT split out the overridden source directory */
            return false;
        }
    }
}
