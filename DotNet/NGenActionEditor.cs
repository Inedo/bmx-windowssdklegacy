using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class NGenActionEditor : ActionEditorBase
    {
        private DropDownList ddlMode;
        private ValidatingTextBox txtTargetAssembly;
        private CheckBox cbQueue;
        private SlimFormField ffgTargetAssembly;
        private SlimFormField ffgOptions;

        public override void BindToForm(ActionBase extension)
        {
            var ngen = (NGenAction)extension;
            this.ddlMode.SelectedValue = ngen.RunMode.ToString();
            this.txtTargetAssembly.Text = ngen.TargetAssembly ?? string.Empty;
            this.cbQueue.Checked = ngen.UseQueue;
            this.HandleModeChange();
        }
        public override ActionBase CreateFromForm()
        {
            return new NGenAction
            {
                RunMode = (NGenMode)Enum.Parse(typeof(NGenMode), this.ddlMode.SelectedValue),
                TargetAssembly = this.txtTargetAssembly.Text,
                UseQueue = this.cbQueue.Checked
            };
        }

        protected override void CreateChildControls()
        {
            this.txtTargetAssembly = new ValidatingTextBox { Required = true };

            this.cbQueue = new CheckBox { Text = "Queue for background generation" };

            this.ddlMode = new DropDownList();
            this.ddlMode.AutoPostBack = true;
            this.ddlMode.Items.Add(new ListItem("Install", "Install"));
            this.ddlMode.Items.Add(new ListItem("Uninstall", "Uninstall"));
            this.ddlMode.Items.Add(new ListItem("Update", "Update"));
            this.ddlMode.SelectedValue = "Install";
            this.ddlMode.SelectedIndexChanged += ddlMode_SelectedIndexChanged;

            this.ffgTargetAssembly = new SlimFormField("Target assembly:", this.txtTargetAssembly)
            {
                HelpText = "The absolute path to the .NET assembly or its strong name if it is installed in the GAC."
            };

            this.ffgOptions = new SlimFormField("Options:", this.cbQueue);

            this.Controls.Add(
                new SlimFormField("Action:", this.ddlMode),
                this.ffgTargetAssembly,
                this.ffgOptions
            );
        }

        private void HandleModeChange()
        {
            switch (this.ddlMode.SelectedValue)
            {
                case "Install":
                    this.ffgTargetAssembly.Visible = true;
                    this.ffgOptions.Visible = true;
                    this.txtTargetAssembly.Required = true;
                    break;

                case "Uninstall":
                    this.ffgTargetAssembly.Visible = true;
                    this.ffgOptions.Visible = false;
                    this.txtTargetAssembly.Required = true;
                    break;

                case "Update":
                    this.ffgTargetAssembly.Visible = false;
                    this.ffgOptions.Visible = true;
                    this.txtTargetAssembly.Required = false;
                    break;
            }
        }

        private void ddlMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleModeChange();
        }
    }
}
