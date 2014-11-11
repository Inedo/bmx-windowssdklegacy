using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    internal sealed class WindowsSdkExtensionConfigurerEditor : ExtensionConfigurerEditorBase
    {
        private ValidatingTextBox txtWindowsSdkPath;
        private ValidatingTextBox txtFrameworkRuntimePath;
        private ValidatingTextBox txtMSBuildToolsPath;

        public override void InitializeDefaultValues()
        {
            bool bm45 = typeof(Util).Assembly.GetName().Version >= new Version(4, 5, 2);
            if (!bm45)
                this.BindToForm(new WindowsSdkExtensionConfigurer());
        }

        public override void BindToForm(ExtensionConfigurerBase extension)
        {
            var configurer = (WindowsSdkExtensionConfigurer)extension;
            this.txtWindowsSdkPath.Text = configurer.WindowsSdkPath;
            this.txtFrameworkRuntimePath.Text = configurer.FrameworkRuntimePath;
            this.txtMSBuildToolsPath.Text = configurer.MSBuildToolsPath;
        }

        public override ExtensionConfigurerBase CreateFromForm()
        {
            return new WindowsSdkExtensionConfigurer
            {
                WindowsSdkPath = Util.NullIf(this.txtWindowsSdkPath.Text.Trim(), string.Empty),
                FrameworkRuntimePath = Util.NullIf(this.txtFrameworkRuntimePath.Text.Trim(), string.Empty),
                MSBuildToolsPath = Util.NullIf(this.txtMSBuildToolsPath.Text.Trim(), string.Empty)
            };
        }

        protected override void CreateChildControls()
        {
            bool bm45 = typeof(Util).Assembly.GetName().Version >= new Version(4, 5, 2);

            this.txtWindowsSdkPath = new ValidatingTextBox
            {
                DefaultText = bm45 ? "latest version in registry" : null
            };

            this.txtFrameworkRuntimePath = new ValidatingTextBox
            {
                DefaultText = bm45 ? "latest installed version" : null
            };

            this.txtMSBuildToolsPath = new ValidatingTextBox
            {
                DefaultText = bm45 ? "latest installed tools path" : null
            };

            this.Controls.Add(
                new SlimFormField("Windows SDK path:", this.txtWindowsSdkPath)
                {
                    HelpText = @"Example: C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A"
                },
                new SlimFormField(".NET runtime path:", this.txtFrameworkRuntimePath)
                {
                    HelpText = @"Example: C:\Windows\Microsoft.NET\Framework64"
                },
                new SlimFormField("MSBuild tools path:", this.txtMSBuildToolsPath)
                {
                    HelpText = @"Example: C:\Program Files (x86)\MSBuild\12.0\bin\amd64"
                }
            );
        }
    }
}
