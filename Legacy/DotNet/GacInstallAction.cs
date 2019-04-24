using System.ComponentModel;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [DisplayName("Install Assemblies into the GAC")]
    [Description("Installs .NET assemblies into the Global Assembly Cache.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(GacInstallActionEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.WindowsSdk.DotNet.GacInstallAction,WindowsSdk")]
    public sealed class GacInstallAction : RemoteActionBase
    {
        [Persistent]
        public string[] FileMasks { get; set; }

        [Persistent]
        public bool ForceRefresh { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Install ",
                    new ListHilite(this.FileMasks),
                    " into the GAC"
                ),
                new RichDescription(
                    "from ",
                    new DirectoryHilite(this.OverriddenSourceDirectory)
                )
            );
        }

        public override bool HasConfigurerSettings()
        {
            return false;
        }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(this.Context.SourceDirectory))
            {
                this.LogError("Invalid configuration; a source path must be provided.");
                return;
            }

            if (this.FileMasks.Length == 0)
            {
                this.LogWarning("Nothing to install into the GAC.");
                return;
            }

            this.ExecuteRemoteCommand("gac");

            this.LogInformation("Installation into the GAC complete.");
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
