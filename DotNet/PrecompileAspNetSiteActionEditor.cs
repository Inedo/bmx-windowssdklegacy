using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Custom editor for the <see cref="PrecompileAspNetSiteAction"/> action.
    /// </summary>
    internal sealed class PrecompileAspNetSiteActionEditor : ActionEditorBase
    {
        private DropDownList ddlVersion;
        private ValidatingTextBox txtVirtualPath;
        private CheckBox chkUpdatable, chkFixedNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecompileAspNetSiteActionEditor"/> class.
        /// </summary>
        public PrecompileAspNetSiteActionEditor()
        {
            ValidateBeforeSave += PrecompileAspNet2SiteEditor_ValidateBeforeSave;
        }

        public override bool DisplaySourceDirectory { get { return true; } }
        public override bool DisplayTargetDirectory { get { return true; } }

        protected override void CreateChildControls()
        {
            ddlVersion = new DropDownList();
            ddlVersion.Items.Add(new ListItem("(auto detect)", ""));
            ddlVersion.Items.Add(new ListItem("2.0", "2.0.50727"));
            ddlVersion.Items.Add(new ListItem("3.5", "3.5"));
            ddlVersion.Items.Add(new ListItem("4.0", "4.0.30319"));

            txtVirtualPath = new ValidatingTextBox { Required = true, Text = "/" };

            chkUpdatable = new CheckBox { Text = "Allow this precompiled site to be updatable" };
            chkFixedNames = new CheckBox { Text = "Use fixed naming and single page assemblies" };

            CUtil.Add(this,
                    new FormFieldGroup(".NET Framework Version",
                    "The version of the .NET Framework to use when building this project.",
                    false,
                    new StandardFormField("Version:", ddlVersion)
                ),
                new FormFieldGroup("Application Virtual Path",
                    "The virtual path of the application to be compiled.",
                    false,
                    new StandardFormField("Application Virtual Path:", txtVirtualPath)
                ),
                new FormFieldGroup("Additional Options",
                    "",
                    true,
                    new StandardFormField("", chkUpdatable),
                    new StandardFormField("", chkFixedNames)
                )
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            EnsureChildControls();

            var action = (PrecompileAspNetSiteAction)extension;
            txtVirtualPath.Text = action.ApplicationVirtualPath;
            ddlVersion.SelectedValue = action.DotNetVersion ?? "";
            chkFixedNames.Checked = action.FixedNames;
            chkUpdatable.Checked = action.Updatable;
        }
        public override ActionBase CreateFromForm()
        {
            EnsureChildControls();

            return new PrecompileAspNetSiteAction
            {
                ApplicationVirtualPath = txtVirtualPath.Text,
                DotNetVersion = ddlVersion.SelectedValue,
                FixedNames = chkFixedNames.Checked,
                Updatable = chkUpdatable.Checked
            };
        }

        private void PrecompileAspNet2SiteEditor_ValidateBeforeSave(object sender, ValidationEventArgs<ActionBase> e)
        {
            var buildAction = (PrecompileAspNetSiteAction)e.Extension;
            if (!buildAction.ApplicationVirtualPath.StartsWith("/"))
            {
                e.ValidLevel = ValidationLevel.Warning;
                e.Message =
                    "Application Virtual Paths should start with \"/\", for example \"/MyApp\". This " +
                    "will likely cause a build error.";
            }
        }
    }
}
