using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Microsoft.Win32;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    public sealed class WindowsSdkExtensionConfigurer : ExtensionConfigurerBase
    {
        private static readonly Regex VersionMatch = new Regex(@"\d+\.\d+", RegexOptions.Compiled | RegexOptions.Singleline);

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsSdkExtensionConfigurer"/> class.
        /// </summary>
        public WindowsSdkExtensionConfigurer()
        {
            this.WindowsSdkPath = GetWindowsSdkInstallRoot() ?? GetDotNetSdkInstallRoot() ?? string.Empty;
            this.FrameworkRuntimePath = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), @"..\"));
        }

        [Persistent]
        public string WindowsSdkPath { get; set; }
        [Persistent]
        public string FrameworkRuntimePath { get; set; }
        [Persistent]
        public string AzureUserName { get; set; }
        [Persistent(Encrypted = true)]
        public string AzurePassword { get; set; }

        /// <summary>
        /// Returns the full path to a specified .NET framework runtime version.
        /// </summary>
        /// <param name="version">Version of .NET requested.</param>
        /// <returns>Full path to the .NET framework runtime.</returns>
        /// <remarks>
        /// Example versions: v2.0.50727, v3.5
        /// </remarks>
        public string GetFrameworkRuntimeVersionPath(string version)
        {
            if (version == null)
                throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(this.FrameworkRuntimePath))
                throw new InvalidOperationException("The .NET Framework runtime path is unknown.");

            return Path.Combine(this.FrameworkRuntimePath, version);
        }

        public override string ToString()
        {
            return string.Empty;
        }

        /// <summary>
        /// Returns the location of the .NET SDK if it is installed.
        /// </summary>
        /// <returns>Path to the .NET SDK if it is installed; otherwise null.</returns>
        private static string GetDotNetSdkInstallRoot()
        {
            RegistryKey key = Registry
                .LocalMachine
                .OpenSubKey(@"SOFTWARE\Microsoft\.NETFramework");
            if (key == null) return null;

            object val = key.GetValue("SDKInstallRootv2.0");
            if (val == null) return null;

            return val.ToString();
        }

        /// <summary>
        /// Returns the location of the Windows SDK if it is installed.
        /// </summary>
        /// <returns>Path to the Windows SDK if it is installed; otherwise null.</returns>
        private static string GetWindowsSdkInstallRoot()
        {
            using (var windowsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SDKs\Windows", false))
            {
                if (windowsKey == null)
                    return null;

                // Later versions of the SDK have this value, but it might not always be there.
                var installFolder = windowsKey.GetValue("CurrentInstallFolder") as string;
                if (!string.IsNullOrEmpty(installFolder))
                    return installFolder;

                var subkeys = windowsKey.GetSubKeyNames();
                if (subkeys.Length == 0)
                    return null;

                // Sort subkeys to find the highest version number.
                Array.Sort<string>(subkeys, (a, b) =>
                {
                    var aMatch = VersionMatch.Match(a);
                    var bMatch = VersionMatch.Match(b);
                    if (!aMatch.Success && !bMatch.Success)
                        return 0;
                    else if (!bMatch.Success)
                        return -1;
                    else if (!aMatch.Success)
                        return 1;
                    else
                        return -new Version(aMatch.Value).CompareTo(new Version(bMatch.Value));
                });

                using (var versionKey = windowsKey.OpenSubKey(subkeys[0], false))
                {
                    return versionKey.GetValue("InstallationFolder") as string;
                }
            }
        }
    }
}
