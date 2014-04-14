using System;
using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    /// <summary>
    /// Custom editor for the <see cref="BuildAspNetProjectAction"/> action.
    /// </summary>
    internal sealed class BuildAspNetProjectActionEditor : ActionEditorBase
    {
        private DropDownList ddlProjectBuildConfiguration;
        private Div divConfig;
        private ValidatingTextBox txtOtherConfig;
        private ValidatingTextBox txtProjectPath;
        private ValidatingTextBox txtAdditionalArguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAspNetProjectActionEditor"/> class.
        /// </summary>
        public BuildAspNetProjectActionEditor()
        {
        }

        /// <summary>
        /// Gets a value indicating whether [display target directory].
        /// </summary>
        public override bool DisplayTargetDirectory { get { return true; } }

        /// <summary>
        /// Binds to form.
        /// </summary>
        public override void BindToForm(ActionBase extension)
        {
            var action = (BuildAspNetProjectAction)extension;

            if (action.ProjectBuildConfiguration == "Debug" || action.ProjectBuildConfiguration == "Release")
            {
                this.ddlProjectBuildConfiguration.SelectedValue = action.ProjectBuildConfiguration;
            }
            else
            {
                this.ddlProjectBuildConfiguration.SelectedValue = "Other";
                this.txtOtherConfig.Text = action.ProjectBuildConfiguration;
            }

            this.txtProjectPath.Text = Path.Combine(action.OverriddenSourceDirectory, action.ProjectPath);
            this.txtAdditionalArguments.Text = action.AdditionalArguments;
        }

        /// <summary>
        /// Creates from form.
        /// </summary>
        public override ActionBase CreateFromForm()
        {
            return new BuildAspNetProjectAction()
            {
                ProjectBuildConfiguration = this.ddlProjectBuildConfiguration.SelectedValue != "Other"
                                            ? this.ddlProjectBuildConfiguration.SelectedValue
                                            : this.txtOtherConfig.Text,
                ProjectPath = Path.GetFileName(this.txtProjectPath.Text),
                AdditionalArguments = this.txtAdditionalArguments.Text,
                OverriddenSourceDirectory = Path.GetDirectoryName(this.txtProjectPath.Text)
            };
        }

        protected override void CreateChildControls()
        {
            this.ddlProjectBuildConfiguration = new DropDownList()
            {
                Items =
                {
                    new ListItem("Debug", "Debug"),
                    new ListItem("Release", "Release"),
                    new ListItem("Other...", "Other")
                }
            };

            this.txtOtherConfig = new ValidatingTextBox() { Width = 150 };
            this.divConfig = new Div() { ID = "divConfig" };
            this.divConfig.Controls.Add(this.txtOtherConfig);

            this.txtProjectPath = new ValidatingTextBox() { Width = 300, Required = true };

            this.txtAdditionalArguments = new ValidatingTextBox() { Width = 300 };

            this.Controls.Add(
                new FormFieldGroup("Project or Solution File Path",
                    "The path to the MVC project file or solution file.<br /><br />This path may be absolute or relative to the default directory.",
                    false,
                    new StandardFormField("Project File:", this.txtProjectPath)
                ),
                new FormFieldGroup("Project Build Configuration",
                    "The build configuration and platform for your project (usually either Debug or Release).",
                    false,
                    new StandardFormField("Project Build Configuration:", this.ddlProjectBuildConfiguration, this.divConfig)
                ),
                new FormFieldGroup("Additional Arguments",
                    "Any additional arguments to pass to MSBuild.",
                    false,
                    new StandardFormField("Additional Arguments:", this.txtAdditionalArguments)
                )
            );
        }

        protected override void OnPreRender(EventArgs e)
        {
            this.Controls.Add(GetClientSideScript(this.ddlProjectBuildConfiguration.ClientID, this.divConfig.ClientID));

            base.OnPreRender(e);
        }

        private RenderJQueryDocReadyDelegator GetClientSideScript(string ddlProjectBuildId, string divConfigId)
        {
            return new RenderJQueryDocReadyDelegator(w => 
                w.Write(
                    "var onload = $('#" + ddlProjectBuildId + "').find('option').filter(':selected').val();" +
                    "if(onload == 'Other')" +
                    "{" +
                        "$('#" + divConfigId + "').show();" +
                    "}" +

                    "$('#" + ddlProjectBuildId + "').change(function () {" +
                        "var selectedConfig = $(this).find('option').filter(':selected').val();" +
                        "if(selectedConfig == 'Other')" +
                        "{" +
                            "$('#" + divConfigId + "').show();" +
                        "}" +
                        "else" +
                        "{" +
                            "$('#" + divConfigId + "').hide();" +
                        "}" +
                    "}).change();"
                ) 
            );
        }
    }
}
