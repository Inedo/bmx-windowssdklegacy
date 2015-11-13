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
                    new BuildMaster.Documentation.DirectoryHilite(config[nameof(this.ProjectPath)])
                ),
                new RichDescription(
                    "with ",
                    new BuildMaster.Documentation.Hilite(config[nameof(this.BuildConfiguration)]),
                    " configuration"
                )
            );
        }

        protected override Task RemoteExecuteAsync(IRemoteOperationExecutionContext context)
        {
            this.LogInformation($"Building {this.ProjectPath}...");

            var projectFullPath = Path.Combine(context.WorkingDirectory, this.ProjectPath);

            var buildProperties = string.Join(";", this.MSBuildProperties ?? Enumerable.Empty<string>());

            var config = "Configuration=" + this.BuildConfiguration;
            if (!string.IsNullOrEmpty(this.TargetPlatform))
                config += ";Platform=" + this.TargetPlatform;

            if (!string.IsNullOrEmpty(buildProperties))
                config += ";" + buildProperties;

            this.LogDebug($"Building {projectFullPath}...");

            var args = $"\"{projectFullPath}\" \"/p:{config}\"";
            if (!string.IsNullOrWhiteSpace(this.TargetDirectory))
                args += $" \"/p:OutDir={this.TargetDirectory.TrimEnd('\\')}\\\\\"";

            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                args += " " + this.AdditionalArguments;

            var workingDir = PathEx.Combine(
                context.WorkingDirectory,
                Path.GetDirectoryName(this.ProjectPath)
            );

            if (!Directory.Exists(workingDir))
                throw new DirectoryNotFoundException($"Directory {workingDir} does not exist.");

            int result = this.InvokeMSBuild(args, workingDir);
            if (result != 0)
                this.LogError($"Build failed (msbuild returned {result}).");

            return Complete;
        }

        private int RunMSBuild(IRemoteOperationExecutionContext context, string argsFormat, params string[] args)
        {
            var allArgs = string.Format(argsFormat, args);

            if (!string.IsNullOrWhiteSpace(this.AdditionalArguments))
                allArgs += " " + this.AdditionalArguments;

            var workingDir = PathEx.Combine(
                context.WorkingDirectory,
                Path.GetDirectoryName(this.ProjectPath)
            );

            if (!Directory.Exists(workingDir))
                throw new DirectoryNotFoundException($"Directory {workingDir} does not exist.");

            return this.InvokeMSBuild(allArgs, workingDir);
        }
        private int InvokeMSBuild(string arguments, string workingDirectory)
        {
            var msbuildProxyPath = Path.Combine(
                Path.GetDirectoryName(typeof(BuildMSBuildProjectOperation).Assembly.Location),
                "BmBuildLogger.exe"
            );

            var msBuildPath = this.GetMSBuildToolsPath();
            if (msBuildPath == null)
                return -1;

            msBuildPath = Path.Combine(msBuildPath, "msbuild.exe");

            var allArgs = $"\"{msBuildPath}\" {arguments}";

            var startInfo = new AgentProcessStartInfo
            {
                FileName = msbuildProxyPath,
                Arguments = allArgs,
                WorkingDirectory = workingDirectory
            };

            using (var process = new LocalTextDataProcess(startInfo))
            {
                process.OutputDataReceived += (s, e) => this.LogProcessOutputData(e.Data);
                process.ErrorDataReceived += (s, e) => this.LogError(e.Data);

                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
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

        private void LogProcessOutputData(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                if (data.StartsWith("!<BM>Info|"))
                    this.LogInformation(data.Substring("!<BM>Info|".Length));
                else if (data.StartsWith("!<BM>Warning|"))
                    this.LogWarning(data.Substring("!<BM>Warning|".Length));
                else
                    this.LogDebug(data);
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
