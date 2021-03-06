﻿using System;
using System.Linq;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    internal sealed class WriteAssemblyInfoVersionsActionEditor : ActionEditorBase
    {
        private ValidatingTextBox txtFileMasks;
        private CheckBox chkRecursive;
        private ValidatingTextBox txtVersion;

        public override bool DisplaySourceDirectory => true;
        public override string SourceDirectoryLabel => "In:";
        public override string ServerLabel => "On:";

        public override void BindToForm(ActionBase extension)
        {
            var action = (WriteAssemblyInfoVersionsAction)extension;
            this.txtFileMasks.Text = string.Join(Environment.NewLine, action.FileMasks ?? new string[0]);
            this.chkRecursive.Checked = action.Recursive;
            this.txtVersion.Text = action.Version;
        }
        public override ActionBase CreateFromForm()
        {
            return new WriteAssemblyInfoVersionsAction
            {
                FileMasks = this.txtFileMasks.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries),
                Recursive = this.chkRecursive.Checked,
                Version = this.txtVersion.Text
            };
        }

        protected override void CreateChildControls()
        {
            var application = DB.Applications_GetApplication(this.ApplicationId).Applications_Extended.FirstOrDefault();
            var variableMode = application != null ? application.VariableSupport_Code : Domains.VariableSupportCodes.All;

            this.txtFileMasks = new ValidatingTextBox
            {
                Required = true,
                TextMode = TextBoxMode.MultiLine,
                Rows = 5,
                Text = "*\\AssemblyInfo.cs"
            };

            this.chkRecursive = new CheckBox
            {
                Text = "Also search in subdirectories"
            };

            this.txtVersion = new ValidatingTextBox
            {
                Required = true,
                Text = variableMode == Domains.VariableSupportCodes.Old ? "%RELNO%.%BLDNO%" : "$ReleaseNumber.$BuildNumber"
            };

            this.Controls.Add(
                new SlimFormField("Assembly version files:", this.txtFileMasks)
                {
                    HelpText = "Use standard BuildMaster file masks (one per line)."
                },
                new SlimFormField("Assembly version:", this.txtVersion),
                new SlimFormField("Options:", this.chkRecursive)
            );
        }
    }
}
