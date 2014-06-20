using System;
using System.IO;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    internal sealed class BuildAspNetProjectActionEditor : ActionEditorBase
    {
        private DropDownList ddlProjectBuildConfiguration;
        private Div divConfig;
        private ValidatingTextBox txtOtherConfig;
        private ValidatingTextBox txtProjectPath;
        private ValidatingTextBox txtAdditionalArguments;

        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }
        public override string TargetDirectoryLabel
        {
            get { return "To:"; }
        }

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
            this.ddlProjectBuildConfiguration = new DropDownList
            {
                Items =
                {
                    new ListItem("Debug", "Debug"),
                    new ListItem("Release", "Release"),
                    new ListItem("Other...", "Other")
                }
            };

            this.txtOtherConfig = new ValidatingTextBox();
            this.divConfig = new Div { ID = "divConfig" };
            this.divConfig.Controls.Add(this.txtOtherConfig);

            this.txtProjectPath = new ValidatingTextBox { Required = true };

            this.txtAdditionalArguments = new ValidatingTextBox();

            this.Controls.Add(
                new SlimFormField("Project file:", this.txtProjectPath),
                new SlimFormField("Build configuration:", this.ddlProjectBuildConfiguration, this.divConfig),
                new SlimFormField("Additional arguments:", this.txtAdditionalArguments)
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
