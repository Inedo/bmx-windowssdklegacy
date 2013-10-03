using System;
using System.IO;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    /// <summary>
    /// Implements common UI elements for building .NET application actions.
    /// </summary>
    public class BuildMSBuildProjectActionEditor : ActionEditorBase
    {
        private DropDownList ddlProjectBuildConfiguration;
        private HtmlGenericControl divConfig;
        private TextBox txtOtherConfig;
        private TextBox txtProjectPath;
        private CheckBox chkWebProject;
        private DropDownList ddlBuildOutputDir;
        private DropDownList ddlVersion;
        private DropDownList ddlProjectBuildTargetPlatform;
        private TextBox txtOtherPlatform;
        private HtmlGenericControl divPlatform;
        private TextBox txtAdditionalProperties;
        private HtmlGenericControl divTargetDir;
        private SourceControlFileFolderPicker txtTargetDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildMSBuildProjectActionEditor"/> class.
        /// </summary>
        public BuildMSBuildProjectActionEditor()
        {
            this.ValidateBeforeSave += new EventHandler<ValidationEventArgs<ActionBase>>(BuildNetAppActionEditor_ValidateBeforeSave);
        }

        protected override void CreateChildControls()
        {
            this.ddlVersion = new DropDownList()
            {
                Items = 
                { 
                    new ListItem("(auto detect)", ""),
                    new ListItem("2.0", "2.0.50727"),
                    new ListItem("3.5", "3.5"),
                    new ListItem("4.0", "4.0.30319")
                }
            };

            this.ddlProjectBuildConfiguration = new DropDownList()
            {
                Items =
                {
                    new ListItem("Debug", "Debug"),
                    new ListItem("Release", "Release"),
                    new ListItem("Other", "Other")
                }
            };

            this.txtOtherConfig = new TextBox() { Width = 150 };

            this.ddlProjectBuildTargetPlatform = new DropDownList()
            {
                Items =
                {
                    new ListItem("(Default)", string.Empty),
                    new ListItem("Any CPU", "AnyCPU"),
                    new ListItem("x86", "x86"),
                    new ListItem("x64", "x64"),
                    new ListItem("Other", "Other")
                }
            };

            this.txtOtherPlatform = new TextBox() { Width = 150 };

            this.txtProjectPath = new TextBox() { Width = 325 };

            this.chkWebProject = new CheckBox()
            {
                Text = "This is a Web Application project."
            };

            this.ddlBuildOutputDir = new DropDownList()
            {
                Items =
                {
                    new ListItem("Specify output directory...", "target"),
                    new ListItem("Per project \\bin\\{config} directories", "bin"),
                }
            };

            this.txtTargetDir = new SourceControlFileFolderPicker()
            {
                ServerId = this.ServerId,
                DefaultText = "default",
                Width = 270
            };

            this.txtAdditionalProperties = new TextBox
            {
                Width = 300,
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            this.Controls.Add(
                new FormFieldGroup(".NET Framework Version",
                    "The version of the .NET Framework to use when building this project.",
                    false,
                    new StandardFormField("Version:", this.ddlVersion)
                )
            );

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
                new FormFieldGroup("Project Build Configuration",
                    "The build configuration and platform for your project (usually either Debug or Release).",
                    false,
                    new StandardFormField("Project Build Configuration:", this.ddlProjectBuildConfiguration, this.divConfig),
                    new StandardFormField("Target Platform:", this.ddlProjectBuildTargetPlatform, this.divPlatform)
                ),
                new FormFieldGroup("Project or Solution File Path",
                    "The path to an msbuild project file or solution file, typically: .csproj, .vbproj, .vcxproj, or .sln. <br /><br />This path may be absolute or relative to the default directory.",
                    false,
                    new StandardFormField("Source Path:", this.txtProjectPath, this.chkWebProject)
                ),
                new FormFieldGroup("Build Output Directory",
                    "The directory of the build output. The \\bin\\{config} option is recommended when building a solution file. If \"This is a Web Application project\" is selected, an output directory must be specified.",
                    false,
                    new StandardFormField("Build Output Directory:", this.ddlBuildOutputDir, this.divTargetDir)
                ),
                new FormFieldGroup("MSBuild Properties",
                    "Additional properties, separated by newlines.  Example:<br />WarningLevel=2<br />Optimize=false",
                    true,
                    new StandardFormField("MSBuild Properties:", this.txtAdditionalProperties)
                )
            );
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.Controls.Add(GetClientSideScript(this.ddlProjectBuildConfiguration.ClientID, this.divConfig.ClientID, this.ddlProjectBuildTargetPlatform.ClientID, this.divPlatform.ClientID, this.chkWebProject.ClientID, this.ddlBuildOutputDir.ClientID, this.divTargetDir.ClientID));

            base.OnPreRender(e);
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

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
            this.ddlVersion.SelectedValue = buildAction.DotNetVersion ?? "";
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
            EnsureChildControls();

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
            buildAction.DotNetVersion = this.ddlVersion.SelectedValue;
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

        private void BuildNetAppActionEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            if (string.IsNullOrEmpty(this.txtProjectPath.Text))
            {
                e.ValidLevel = ValidationLevel.Warning;
                e.Message = "Project path is not set. This may result in build errors.";
            }
        }

        private LiteralControl GetClientSideScript(string ddlProjectBuildId, string divConfigId, string ddlTargetPlatformId, string divPlatformId, string chkWebProjectId, string ddlBuildOutputDirId, string divTargetDirId)
        {
            return new LiteralControl(
                "<script type=\"text/javascript\">" +
                    "$(document).ready(function(){" +
                        "var onload = $('#" + ddlProjectBuildId + "').find('option').filter(':selected').text();" +
                        "if(onload == 'Other')" +
                        "{" +
                            "$('#" + divConfigId + "').show();" +
                        "}" +

                        "$('#" + ddlProjectBuildId + "').change(function () {" +
                            "var selectedConfig = $(this).find('option').filter(':selected').text();" +
                            "if(selectedConfig == 'Other')" +
                            "{" +
                                "$('#" + divConfigId + "').show();" +
                            "}" +
                            "else" +
                            "{" +
                                "$('#" + divConfigId + "').hide();" +
                            "}" +
                        "});" +

                        "var onload2 = $('#" + ddlTargetPlatformId + "').find('option').filter(':selected').text();" +
                        "if(onload2 == 'Other')" +
                        "{" +
                            "$('#" + divPlatformId + "').show();" +
                        "}" +

                        "$('#" + ddlTargetPlatformId + "').change(function () {" +
                            "var selectedConfig = $(this).find('option').filter(':selected').text();" +
                            "if(selectedConfig == 'Other')" +
                            "{" +
                                "$('#" + divPlatformId + "').show();" +
                            "}" +
                            "else" +
                            "{" +
                                "$('#" + divPlatformId + "').hide();" +
                            "}" +
                        "});" +

                        "$('#" + chkWebProjectId + "').change(function () {" +
                            "if ($(this).is(':checked')) {" +
                                "$('#" + ddlBuildOutputDirId + "').val('target').attr('disabled', 'disabled');" +
                                "$('#" + divTargetDirId + "').show();" +
                            "}" +
                            "else {" +
                                "$('#" + ddlBuildOutputDirId + "').removeAttr('disabled');" +
                            "}" +
                        "});" +

                        "$('#" + ddlBuildOutputDirId + "').change(function () {" +
                            "if($(this).val() == 'target')" +
                            "{" +
                                "$('#" + divTargetDirId + "').show();" +
                            "}" +
                            "else" +
                            "{" +
                                "$('#" + divTargetDirId + "').hide();" +
                            "}" +
                        "}).change();" +

                    "});" +
                "</script>");
        }
    }
}
