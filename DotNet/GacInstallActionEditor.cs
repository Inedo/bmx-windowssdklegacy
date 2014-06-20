using System;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

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
        public override string SourceDirectoryLabel
        {
            get { return "In:"; }
        }
        public override string ServerLabel
        {
            get { return "On:"; }
        }

        protected override void CreateChildControls()
        {
            this.txtGacFiles = new TextBox
            {
                TextMode = TextBoxMode.MultiLine,
                Rows = 4
            };

            this.chkForceRefresh = new CheckBox
            {
                Text = "Force refresh",
                Checked = true
            };

            this.Controls.Add(
                new SlimFormField("Files:", this.txtGacFiles)
                {
                    HelpText = "Files or masks listed (entered one per line) will be added to the GAC."
                },
                new SlimFormField("Options:", this.chkForceRefresh)
            );
        }

        public override void BindToForm(ActionBase extension)
        {
            var gac = (GacInstallAction)extension;
            this.txtGacFiles.Text = string.Join(Environment.NewLine, gac.FileMasks);
            this.chkForceRefresh.Checked = gac.ForceRefresh;
        }

        public override ActionBase CreateFromForm()
        {
            return new GacInstallAction
            {
                FileMasks = Regex.Split(this.txtGacFiles.Text, Environment.NewLine),
                ForceRefresh = this.chkForceRefresh.Checked
            };
        }
    }
}
