using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    public sealed class WindowsSdkExtensionConfigurer : ExtensionConfigurerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsSdkExtensionConfigurer"/> class.
        /// </summary>
        public WindowsSdkExtensionConfigurer()
        {
        }

        [Persistent]
        public string WindowsSdkPath { get; set; }
        [Persistent]
        public string FrameworkRuntimePath { get; set; }

        public override string ToString()
        {
            return string.Empty;
        }
    }
}
