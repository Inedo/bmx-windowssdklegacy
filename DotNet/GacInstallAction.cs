using System;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Represents an action that installs a .NET assembly into the Global Assembly Cache.
    /// </summary>
    [ActionProperties(
        "Install Assemblies into the GAC",
        "Installs .NET assemblies into the Global Assembly Cache.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(GacInstallActionEditor))]
    public sealed class GacInstallAction : RemoteActionBase
    {
        /// <summary>
        /// See <see cref="object.ToString()"/>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("Install {0} from {1} into the GAC{2}", 
                String.Join(", ", FileMasks),
                (String.IsNullOrEmpty(OverriddenSourceDirectory)
                    ? "default directory."
                    : OverriddenSourceDirectory),
                (this.ForceRefresh) ? " (force refresh)" : ""
            );
        }

        /// <summary>
        /// Gets or sets an array of files to install to the GAC.
        /// </summary>
        [Persistent]
        public string[] FileMasks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a refresh is forced.
        /// </summary>
        /// <remarks>
        /// A forced refresh indicates that the GAC'ed assembly will be updated
        /// even if the version number has not changed.
        /// </remarks>
        [Persistent]
        public bool ForceRefresh { get; set; }

        public override bool HasConfigurerSettings()
        {
            return false;
        }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(this.Context.SourceDirectory))
            {
                LogError("Invalid configuration; a source path must be provided.");
                return;
            }

            if (FileMasks.Length == 0)
            {
                LogInformation("Nothing to install into the GAC.");
                return;
            }

            ExecuteRemoteCommand("gac");

            LogInformation("Installation into the GAC complete");
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var allFiles = Util.Files.GetDirectoryEntry(new BuildMaster.Files.GetDirectoryEntryCommand
            {
                Path = this.Context.SourceDirectory,
                Recurse = false,
                IncludeRootPath = true
            });

            var allMatches = Util.Files.Comparison.GetMatches(this.Context.SourceDirectory, allFiles.Entry, this.FileMasks);

            foreach (var file in allMatches)
                AssemblyCache.InstallAssembly(file.Path, null, this.ForceRefresh ? AssemblyCommitFlags.Force : AssemblyCommitFlags.Default);

            return string.Empty;
        }
    }
}
