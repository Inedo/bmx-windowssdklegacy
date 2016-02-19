using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class PrecompileAspNetSiteActionEditor : ActionEditorBase
    {
        private DropDownList ddlVersion;
        private ValidatingTextBox txtVirtualPath;
        private CheckBox chkUpdatable;
        private CheckBox chkFixedNames;

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }
        public override string SourceDirectoryLabel
        {
            get { return "In:"; }
        }
        public override bool DisplayTargetDirectory
        {
            get { return true; }
        }
        public override string TargetDirectoryLabel
        {
            get { return "To:"; }
        }
        public override string ServerLabel
        {
            get { return "On:"; }
        }

        protected override void CreateChildControls()
        {
            this.ddlVersion = new DropDownList();
            this.ddlVersion.Items.Add(new ListItem("(auto detect)", ""));
            this.ddlVersion.Items.Add(new ListItem("2.0", "2.0.50727"));
            this.ddlVersion.Items.Add(new ListItem("3.5", "3.5"));
            this.ddlVersion.Items.Add(new ListItem("4.0", "4.0.30319"));

            this.txtVirtualPath = new ValidatingTextBox { Required = true, Text = "/" };

            this.chkUpdatable = new CheckBox { Text = "Allow this precompiled site to be updatable" };

            this.chkFixedNames = new CheckBox { Text = "Use fixed naming and single page assemblies" };

            this.Controls.Add(
                new SlimFormField(".NET version:", this.ddlVersion),
                new SlimFormField("Virtual path:", this.txtVirtualPath),
                new SlimFormField(
                    "Options:",
                    new Div(this.chkUpdatable),
                    new Div(this.chkFixedNames)
                )
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            var action = (PrecompileAspNetSiteAction)extension;
            this.txtVirtualPath.Text = action.ApplicationVirtualPath;
            this.ddlVersion.SelectedValue = action.DotNetVersion ?? "";
            this.chkFixedNames.Checked = action.FixedNames;
            this.chkUpdatable.Checked = action.Updatable;
        }
        public override ActionBase CreateFromForm()
        {
            return new PrecompileAspNetSiteAction
            {
                ApplicationVirtualPath = this.txtVirtualPath.Text,
                DotNetVersion = this.ddlVersion.SelectedValue,
                FixedNames = this.chkFixedNames.Checked,
                Updatable = this.chkUpdatable.Checked
            };
        }
    }
}
