using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Diagnostics;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Diagnostics;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    /// <summary>
    /// Common base class for all .NET build actions.
    /// </summary>
    public abstract class MSBuildActionBase : RemoteActionBase
    {
        /// <summary>
        /// Expression used for matching installed .NET framework versions.
        /// </summary>
        private static readonly Regex VersionMatcher = new Regex(@"^v\d+\.\d+(\.\d+)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private Action<string> logOutputData;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSBuildActionBase"/> class.
        /// </summary>
        protected MSBuildActionBase()
        {
        }

        /// <summary>
        /// Gets or sets the .NET framework version used by this action.
        /// </summary>
        /// <remarks>
        /// A null or empty value indicates that the most recent version will be used.
        /// Otherwise, the string should be in the form: Major.Minor[.build], where [.build] is optional.
        /// </remarks>
        /// <example>
        /// 3.5, 4.0.30319
        /// </example>
        [Persistent]
        public string DotNetVersion { get; set; }

        /// <summary>
        /// Returns the full path to the desired .NET framework version installation.
        /// </summary>
        /// <returns>Full path to the desired .NET framework version installation.</returns>
        protected string GetFrameworkPath()
        {
            var frameworkPath = ((WindowsSdkExtensionConfigurer)this.GetExtensionConfigurer()).FrameworkRuntimePath;
            if (string.IsNullOrEmpty(frameworkPath) || !Directory.Exists(frameworkPath))
                throw new InvalidOperationException(".NET framework runtime path is not valid. Verify .NET extension configuration.");

            var versions = GetInstalledVersions(frameworkPath);
            if (versions.Count == 0)
                throw new InvalidOperationException(".NET framework runtime path is not valid. Verify .NET extension configuration.");

            // Return the highest installed version if not specified.
            if (string.IsNullOrEmpty(this.DotNetVersion))
                return Path.Combine(frameworkPath, "v" + versions[versions.Count - 1].ToString());

            // Otherwise try to match the desired version number.
            Version desiredVersion;
            try
            {
                desiredVersion = new Version(this.DotNetVersion);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Target .NET framework version {0} is not a valid version number.", this.DotNetVersion), ex);
            }

            // First look for an exact match.
            var exactMatch = versions.Find(v => v.Equals(desiredVersion));
            if (exactMatch != null)
                return Path.Combine(frameworkPath, "v" + exactMatch.ToString());

            // Now try to match by major and minor versions only, favoring newer versions first.
            var match = versions.FindLast(v => v.Major == desiredVersion.Major && v.Minor == desiredVersion.Minor);
            if (match != null)
                return Path.Combine(frameworkPath, "v" + match.ToString());

            throw new InvalidOperationException(string.Format("Target .NET framework version {0} was not found in {1}.", this.DotNetVersion, frameworkPath));
        }
        /// <summary>
        /// Gets the path to msbuild.exe.
        /// </summary>
        /// <returns>Full path to msbuild.exe.</returns>
        protected string GetMSBuildPath()
        {
            return Path.Combine(GetFrameworkPath(), "msbuild.exe");
        }
        /// <summary>
        /// Launches the MSBuild process.
        /// </summary>
        /// <param name="arguments">The arguments to pass to MSBuild.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>MSBuild exit code.</returns>
        protected int InvokeMSBuild(string arguments, string workingDirectory)
        {
            try
            {
                var consoleOutput = new List<string>();
                this.logOutputData = d => consoleOutput.Add(d);
                bool useConsoleOutput = false;

                string loggerPath;
                string logFile;
                string allArgs;

                try
                {
                    loggerPath = this.UnpackLogger();
                    logFile = Path.Combine(this.Context.TempDirectory, Guid.NewGuid().ToString("N"));

                    allArgs = string.Format("/logger:\"{0}\";\"{1}\" ", loggerPath, logFile) + arguments;
                }
                catch (Exception ex)
                {
                    allArgs = arguments;
                    useConsoleOutput = true;
                    logFile = null;
                    this.LogWarning("Unable to set up custom build logging (Error: {0}); falling back on msbuild console output.", ex.Message);
                }

                int exitCode = this.ExecuteCommandLine(this.GetMSBuildPath(), allArgs, workingDirectory);

                if (!useConsoleOutput)
                {
                    try
                    {
                        // Read the log messages
                        var logDoc = new XmlDocument();
                        logDoc.Load(logFile);

                        var nodes = logDoc.SelectNodes("/buildLog/event");
                        foreach (XmlElement node in nodes)
                        {
                            var level = (MessageLevel)Enum.Parse(typeof(MessageLevel), node.GetAttribute("level"));
                            var message = node.GetAttribute("message");

                            this.Log(level, message);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.LogWarning("Unable to access custom build log file (Error: {0}); falling back on msbuild console output.", ex.Message);
                        useConsoleOutput = true;
                    }
                }

                if (useConsoleOutput)
                {
                    foreach (var line in consoleOutput)
                        this.LogDebug(line);
                }

                return exitCode;
            }
            finally
            {
                this.logOutputData = null;
            }
        }

        /// <summary>
        /// Invoked when data is written to the process's Standard Out output.
        /// </summary>
        /// <param name="data">Data written to Standard Out.</param>
        protected override void LogProcessOutputData(string data)
        {
            if (this.logOutputData != null)
            {
                if (!string.IsNullOrEmpty(data))
                    this.logOutputData(data);
            }
            else
            {
                base.LogProcessOutputData(data);
            }
        }

        /// <summary>
        /// Returns a list of available .NET framework runtime versions.
        /// </summary>
        /// <param name="frameworkPath">.NET framework root path.</param>
        /// <returns>List of available versions, sorted by version number.</returns>
        private List<Version> GetInstalledVersions(string frameworkPath)
        {
            var frameworkDirs = new DirectoryInfo(frameworkPath).GetDirectories("v*");
            var versions = new List<Version>();

            foreach (var dir in frameworkDirs)
            {
                if (VersionMatcher.IsMatch(dir.Name))
                    versions.Add(new Version(dir.Name.Substring(1)));
            }

            versions.Sort();
            return versions;
        }
        /// <summary>
        /// Extracts the MSBuild custom logger assembly to a temp path.
        /// </summary>
        /// <returns>The path of the custom logger assembly.</returns>
        private string UnpackLogger()
        {
            var assemblyPath = Path.Combine(this.Context.TempDirectory, "XmlBuildLogger.dll");
            using (var input = typeof(MSBuildActionBase).Assembly.GetManifestResourceStream("Inedo.BuildMasterExtensions.DotNet2.XmlBuildLogger.dll"))
            using (var output = File.Create(assemblyPath))
            {
                var buffer = new byte[4096];
                int len = input.Read(buffer, 0, buffer.Length);
                while (len > 0)
                {
                    output.Write(buffer, 0, len);
                    len = input.Read(buffer, 0, buffer.Length);
                }
            }

            return assemblyPath;
        }
    }
}
