using System;
using System.ComponentModel;
using System.IO;
using System.Web;
using Inedo.BuildMaster;
using Inedo.Documentation;
using Inedo.IO;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.MSBuild
{
    [DisplayName("Build ASP.NET Web or MVC Project")]
    [Description("Builds an ASP.NET Web Application or ASP.NET MVC project and applies .config transforms.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(BuildAspNetProjectActionEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.WindowsSdk.MSBuild.BuildAspNetProjectAction,WindowsSdk")]
    public sealed class BuildAspNetProjectAction : MSBuildActionBase
    {
        [Persistent]
        public string ProjectBuildConfiguration { get; set; }

        [Persistent]
        public string ProjectPath { get; set; }

        [Persistent]
        public string AdditionalArguments { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Build MVC Project ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.ProjectPath)
                ),
                new RichDescription(
                    "with ",
                    new Hilite(this.ProjectBuildConfiguration),
                    " configuration to ",
                    new DirectoryHilite(this.OverriddenTargetDirectory)
                )
            );
        }

        protected override void Execute()
        {
            this.ExecuteRemoteCommand(null);
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            int exitCode = this.InvokeMSBuild(
                string.Format(
                    "\"{0}\" /t:Rebuild /p:Configuration={1};DeployOnBuild=True;BaseIntermediateOutputPath={2} {3}",
                    Path.Combine(this.Context.SourceDirectory, Path.GetFileName(this.ProjectPath)),
                    this.ProjectBuildConfiguration,
                    EnsureTrailingSlash(HttpUtility.UrlPathEncode(this.Context.TempDirectory)),
                    this.AdditionalArguments
                ),
                this.Context.SourceDirectory
            );

            // Output files are located in ..\{BuildConfiguration}\Package\PackageTmp
            string outputPath = Path.Combine(this.Context.TempDirectory, this.ProjectBuildConfiguration, @"Package\PackageTmp");

            if (!Directory.Exists(outputPath))
                throw new InvalidOperationException("There are no files in the expected output directory: " + outputPath);

            this.LogDebug($"Moving files from {outputPath} to target directory: {this.Context.TargetDirectory} ");
            foreach (var item in new DirectoryInfo(outputPath).EnumerateFileSystemInfos())
            {
                var relativePath = item.FullName.Substring(outputPath.Length).TrimStart('\\', '/');
                var targetPath = PathEx.Combine(this.Context.TargetDirectory, relativePath);
                Directory.CreateDirectory(PathEx.GetDirectoryName(targetPath));

                var fileInfo = item as FileInfo;
                if (fileInfo != null)
                {
                    fileInfo.MoveTo(targetPath);
                }
                else
                {
                    var directoryInfo = item as DirectoryInfo;
                    if (directoryInfo != null)
                        directoryInfo.MoveTo(targetPath);
                }
            }

            return null;
        }

        private static string EnsureTrailingSlash(string path)
        {
            return path.TrimEnd('\\') + "\\";
        }
    }
}
