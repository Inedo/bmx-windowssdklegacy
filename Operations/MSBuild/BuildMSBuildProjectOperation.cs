using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;
using Microsoft.Win32;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Operations.MSBuild
{
    [Serializable]
    [Tag(Tags.DotNet)]
    [ScriptAlias("Build-Project")]
    [DisplayName("Build MSBuild Project")]
    [Description("Builds a project or solution using MSBuild.")]
    [ScriptNamespace("MSBuild")]
    [DefaultProperty(nameof(ProjectPath))]
    public sealed class BuildMSBuildProjectOperation : RemoteExecuteOperation
    {
        [Required]
        [ScriptAlias("Configuration")]
        [DefaultValue("Release")]
        public string BuildConfiguration { get; set; }

        [ScriptAlias("Platform")]
        public string TargetPlatform { get; set; }

        [Required]
        [ScriptAlias("ProjectFile")]
        public string ProjectPath { get; set; }

        [ScriptAlias("MSBuildProperties")]
        public IEnumerable<string> MSBuildProperties { get; set; }

        [ScriptAlias("Arguments")]
        public string AdditionalArguments { get; set; }

        [ScriptAlias("MSBuildToolsPath")]
        [DefaultValue("$MSBuildToolsPath")]
        public string MSBuildToolsPath { get; set; }

        [ScriptAlias("To")]
        public string TargetDirectory { get; set; }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Build ",
                    new DirectoryHilite(config[nameof(this.ProjectPath)])
                ),
                new RichDescription(
                    "with ",
                    new Hilite(config[nameof(this.BuildConfiguration)]),
                    " configuration"
                )
            );
        }

        protected override async Task RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        {
            //if (!string.IsNullOrWhiteSpace(this.TargetDirectory))
            //{
            //    var target = context.ResolvePath(this.TargetDirectory);
            //    this.LogDebug($"Clearing {target}...");
            //    DirectoryEx.Clear(target);
            //    this.LogDebug(target + " cleared.");
            //}

            var projectFullPath = context.ResolvePath(this.ProjectPath);

            this.LogInformation($"Building {projectFullPath}...");

            var buildProperties = string.Join(";", this.MSBuildProperties ?? Enumerable.Empty<string>());

            var config = "Configuration=" + this.BuildConfiguration;
            if (!string.IsNullOrEmpty(this.TargetPlatform))
                config += ";Platform=" + this.TargetPlatform;

            if (!string.IsNullOrEmpty(buildProperties))
                config += ";" + buildProperties;

            var args = $"\"{projectFullPath}\" \"/p:{config}\"";
            if (!string.IsNullOrWhiteSpace(this.TargetDirectory))
                args += $" \"/p:OutDir={context.ResolvePath(this.TargetDirectory).TrimEnd('\\')}\\\\\"";

            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                args += " " + this.AdditionalArguments;

            var workingDir = PathEx.GetDirectoryName(projectFullPath);

            if (!DirectoryEx.Exists(workingDir))
                throw new DirectoryNotFoundException($"Directory {workingDir} does not exist.");

            int result = await this.InvokeMSBuildAsync(context, args, workingDir);
            if (result != 0)
                this.LogError($"Build failed (msbuild returned {result}).");
        }

        private async Task<int> InvokeMSBuildAsync(IRemoteOperationExecutionContext context, string arguments, string workingDirectory)
        {
            var msbuildLoggerPath = Path.Combine(
                Path.GetDirectoryName(typeof(BuildMSBuildProjectOperation).Assembly.Location),
                "BmBuildLogger.dll"
            );

            var allArgs = $"\"/logger:{msbuildLoggerPath}\" /noconsolelogger " + arguments;

            var msBuildPath = this.GetMSBuildToolsPath();
            if (msBuildPath == null)
                return -1;

            msBuildPath = Path.Combine(msBuildPath, "msbuild.exe");

            var startInfo = new AgentProcessStartInfo
            {
                FileName = msBuildPath,
                Arguments = allArgs,
                WorkingDirectory = workingDirectory
            };

            return await this.ExecuteCommandLineAsync(context, startInfo);
        }
        private string GetMSBuildToolsPath()
        {
            if (!string.IsNullOrWhiteSpace(this.MSBuildToolsPath))
            {
                this.LogDebug("MSBuildToolsPath: " + this.MSBuildToolsPath);
                return this.MSBuildToolsPath;
            }

            this.LogInformation("$MSBuildToolsPath variable is not set. Attempting to find latest version from the registry...");

            string path = null;

            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\MSBuild\ToolsVersions", false))
            {
                if (key != null)
                {

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
                        path = subkey.GetValue("MSBuildToolsPath") as string;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                this.LogError(@"Could not determine MSBuildToolsPath value on this server. To resolve this issue, ensure that MSBuild is available on this server and create a server-scoped variable named $MSBuildToolsPath set to the location of the MSBuild tools. For example, the tools included with Visual Studio 2015 are usually installed to C:\Program Files (x86)\MSBuild\14.0\Bin");
                return null;
            }

            this.LogDebug("MSBuildToolsPath: " + path);

            return path;
        }

        protected override void LogProcessOutput(string text)
        {
            if (!string.IsNullOrWhiteSpace(text) && text.StartsWith("<BM>"))
            {
                var bytes = Convert.FromBase64String(text.Substring("<BM>".Length));
                var message = InedoLib.UTF8Encoding.GetString(bytes, 1, bytes.Length - 1);
                this.Log((MessageLevel)bytes[0], message);
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
