using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Inedo.BuildMaster.Extensibility.Configurers.Extension;
using Inedo.Serialization;
using Inedo.Web;
using Microsoft.Win32;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    [CustomEditor(typeof(WindowsSdkExtensionConfigurerEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.WindowsSdk.WindowsSdkExtensionConfigurer,WindowsSdk")]
    public sealed class WindowsSdkExtensionConfigurer : ExtensionConfigurerBase
    {
        private static readonly Regex VersionMatch = new Regex(@"\d+\.\d+", RegexOptions.Compiled | RegexOptions.Singleline);

        public WindowsSdkExtensionConfigurer()
        {
            this.WindowsSdkPath = GetWindowsSdkInstallRoot() ?? GetDotNetSdkInstallRoot() ?? string.Empty;
            this.FrameworkRuntimePath = Path.GetFullPath(Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), @"..\"));
            this.MSBuildToolsPath = GetLatestToolsVersionPath();
        }

        [Persistent]
        public string WindowsSdkPath { get; set; }
        [Persistent]
        public string FrameworkRuntimePath { get; set; }
        [Persistent]
        public string MSBuildToolsPath { get; set; }
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
        internal static string GetWindowsSdkInstallRoot()
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

        private static string GetLatestToolsVersionPath()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions", false))
            {
                if (key == null)
                    return null;

                var latestVersion = key
                    .GetSubKeyNames()
                    .Select(k => new { Key = k, Version = TryParse(k) })
                    .Where(v => v.Version != null)
                    .OrderByDescending(v => v.Version)
                    .FirstOrDefault();

                if (latestVersion == null)
                    return null;

                using (var subkey = key.OpenSubKey(latestVersion.Key, false))
                {
                    return subkey.GetValue("MSBuildToolsPath") as string;
                }
            }
        }

        private static Version TryParse(string s)
        {
            Version v;
            Version.TryParse(s, out v);
            return v;
        }
    }
}
