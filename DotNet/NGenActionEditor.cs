using System;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Provides an editor UI for the NGen action.
    /// </summary>
    internal sealed class NGenActionEditor : ActionEditorBase
    {
        private DropDownList ddlMode;
        private ValidatingTextBox txtTargetAssembly;
        private CheckBox cbQueue;
        private FormFieldGroup ffgTargetAssembly;
        private FormFieldGroup ffgOptions;

        public override void BindToForm(ActionBase extension)
        {
            if (extension == null)
                throw new ArgumentNullException("extension");

            EnsureChildControls();

            var ngen = (NGenAction)extension;
            this.ddlMode.SelectedValue = ngen.RunMode;
            this.txtTargetAssembly.Text = ngen.TargetAssembly ?? string.Empty;
            this.cbQueue.Checked = ngen.UseQueue;
            HandleModeChange();
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new NGenAction()
            {
                RunMode = this.ddlMode.SelectedValue,
                TargetAssembly = this.txtTargetAssembly.Text,
                UseQueue = this.cbQueue.Checked
            };
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtTargetAssembly = new ValidatingTextBox()
            {
                Width = 300,
                Required = true
            };

            this.cbQueue = new CheckBox()
            {
                Text = "Queue for background generation"
            };

            this.ddlMode = new DropDownList();
            this.ddlMode.AutoPostBack = true;
            this.ddlMode.Items.Add(new ListItem("Install", "Install"));
            this.ddlMode.Items.Add(new ListItem("Uninstall", "Uninstall"));
            this.ddlMode.Items.Add(new ListItem("Update", "Update"));
            this.ddlMode.SelectedValue = "Install";
            this.ddlMode.SelectedIndexChanged += ddlMode_SelectedIndexChanged;

            this.ffgTargetAssembly = new FormFieldGroup(
                "Target Assembly",
                "The absolute path to the .NET assembly or its strong name if it is installed in the GAC.",
                false,
                new StandardFormField("Absolute path or strong name of assembly:", this.txtTargetAssembly));

            this.ffgOptions = new FormFieldGroup(
                "Options",
                "Additional options for NGen.",
                true,
                new StandardFormField(string.Empty, this.cbQueue));

            CUtil.Add(this,
                new FormFieldGroup(
                    "Action",
                    "The NGen action to perform.",
                    false,
                    new StandardFormField("Action:", this.ddlMode)
                    ),
                this.ffgTargetAssembly,
                this.ffgOptions);
        }

        /// <summary>
        /// Updates various child controls when the selected mode has changed.
        /// </summary>
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

        /// <summary>
        /// Handles the SelectedIndexChanged event of the ddlMode control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ddlMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            HandleModeChange();
        }
    }
}
