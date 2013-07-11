using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class GacInstallActionEditor : ActionEditorBase
    {
        private TextBox txtGacFiles;
        private CheckBox chkForceRefresh;

        public override bool DisplaySourceDirectory
        {
            get { return true; }
        }

        protected override void CreateChildControls()
        {
            this.txtGacFiles = new TextBox();
            txtGacFiles.TextMode = TextBoxMode.MultiLine;
            txtGacFiles.Rows = 4;
            txtGacFiles.Columns = 30;
            txtGacFiles.Width = Unit.Pixel(300);

            this.chkForceRefresh = new CheckBox();
            chkForceRefresh.Text = "Force refresh";
            chkForceRefresh.Checked = true;

            var GacFilesFieldGroup =
                new FormFieldGroup("Files",
                    "Files listed (entered one per line) will be added to the GAC.",
                    true,
                    new StandardFormField("Files:", txtGacFiles),
                    new StandardFormField("", chkForceRefresh)
                );

            this.Controls.Add(GacFilesFieldGroup);
        }

        public override void BindToForm(ActionBase extension)
        {
            this.EnsureChildControls();

            var gac = (GacInstallAction)extension;
            txtGacFiles.Text = string.Join(Environment.NewLine, gac.FileMasks);
            chkForceRefresh.Checked = gac.ForceRefresh;
        }

        public override ActionBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new GacInstallAction
            {
                FileMasks = Regex.Split(txtGacFiles.Text, Environment.NewLine),
                ForceRefresh = chkForceRefresh.Checked
            };
        }
    }
}
