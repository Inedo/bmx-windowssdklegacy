using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    internal sealed class BuildMSBuildProjectActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtProjectBuildConfiguration;
        private FileBrowserTextBox txtProjectPath;
        private CheckBox chkWebProject;
        private Div divWebProject;
        private DropDownList ddlBuildOutputDir;
        private ValidatingTextBox txtProjectBuildTargetPlatform;
        private TextBox txtAdditionalProperties;
        private FileBrowserTextBox txtTargetDir;
        private ValidatingTextBox txtAdditionalArguments;

        protected override void CreateChildControls()
        {
            this.txtProjectBuildConfiguration = new ValidatingTextBox
            {
                ID = "txtProjectBuildConfiguration",
                AutoCompleteValues = new[] { "Debug", "Release" },
                Required = true,
                Text = "Release"
            };

            this.txtProjectBuildTargetPlatform = new ValidatingTextBox
            {
                ID = "txtProjectBuildTargetPlatform",
                AutoCompleteValues = new[] { "AnyCPU", "Any CPU", "x86", "x64", "Win32" },
                DefaultText = "(default)"
            };

            this.txtProjectPath = new FileBrowserTextBox
            {
                ID = "txtProjectPath",
                IncludeFiles = true
            };

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

            this.txtTargetDir = new FileBrowserTextBox
            {
                DefaultText = "$CurrentDirectory"
            };

            this.txtAdditionalProperties = new ValidatingTextBox
            {
                TextMode = TextBoxMode.MultiLine,
                DefaultText = "(none)",
                Rows = 5
            };

            var divTargetDir = new Div(
                this.txtTargetDir
            ) { ID = "divTargetDir" };

            this.divWebProject = new Div(
                this.chkWebProject,
                new Div(
                    "This option will be removed in a future version of the extension. Use the Build ASP.NET Project action instead."
                ) { Style = "font-size: 9px; margin-left: 20px;" }
            ) { IsIdRequired = false, Visible = false, Style = "margin-top: 10px;" };

            this.txtAdditionalArguments = new ValidatingTextBox
            {
                ID = "txtAdditionalArguments",
                DefaultText = "(none)"
            };

            this.Controls.Add(
                new SlimFormField("Project/solution file:", this.txtProjectPath, this.divWebProject),
                new SlimFormField("Configuration:", this.txtProjectBuildConfiguration),
                new SlimFormField("Platform:", this.txtProjectBuildTargetPlatform),
                new SlimFormField("Output directory:", this.ddlBuildOutputDir, divTargetDir)
                {
                    HelpText = "The directory of the build output. The \\bin\\{config} option is recommended when building a solution file."
                },
                new SlimFormField("MSBuild properties:", this.txtAdditionalProperties)
                {
                    HelpText = "Additional properties, separated by newlines. Example:<br />WarningLevel=2<br />Optimize=false"
                },
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments),
                new RenderJQueryDocReadyDelegator(
                    w =>
                    {
                        w.Write("$('#{0}').change(function(){{if($(this).val()=='target')$('#{1}').show();else $('#{1}').hide();}});", this.ddlBuildOutputDir.ClientID, divTargetDir.ClientID);
                        w.Write("$('#{0}').change();", this.ddlBuildOutputDir.ClientID);
                    }
                )
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            var buildAction = (BuildMSBuildProjectAction)extension;

            this.txtProjectBuildConfiguration.Text = buildAction.ProjectBuildConfiguration;
            this.txtProjectBuildTargetPlatform.Text = buildAction.ProjectTargetPlatform;
            this.txtProjectPath.Text = Path.Combine(buildAction.OverriddenSourceDirectory ?? string.Empty, buildAction.ProjectPath);
            this.chkWebProject.Checked = buildAction.IsWebProject;
            this.divWebProject.Visible = buildAction.IsWebProject;
            this.txtAdditionalProperties.Text = buildAction.MSBuildProperties ?? string.Empty;
            this.txtAdditionalArguments.Text = buildAction.AdditionalArguments;
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
            var buildAction = new BuildMSBuildProjectAction
            {
                ProjectBuildConfiguration = Util.NullIf(this.txtProjectBuildConfiguration.Text, string.Empty),
                ProjectTargetPlatform = Util.NullIf(this.txtProjectBuildTargetPlatform.Text, string.Empty),
                AdditionalArguments = Util.NullIf(this.txtAdditionalArguments.Text, string.Empty),
                IsWebProject = this.chkWebProject.Checked
            };

            if (SeparateOverriddenSourceDirectory(this.txtProjectPath.Text, this.txtTargetDir.Text))
            {
                buildAction.OverriddenSourceDirectory = Path.GetDirectoryName(this.txtProjectPath.Text);
                buildAction.ProjectPath = Path.GetFileName(this.txtProjectPath.Text);
            }
            else
            {
                buildAction.ProjectPath = this.txtProjectPath.Text;
            }

            buildAction.MSBuildProperties = this.txtAdditionalProperties.Text;

            if (this.ddlBuildOutputDir.SelectedValue == "bin" && !this.chkWebProject.Checked)
            {
                buildAction.BuildToProjectConfigSubdirectories = true;
                buildAction.OverriddenTargetDirectory = string.Empty;
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
