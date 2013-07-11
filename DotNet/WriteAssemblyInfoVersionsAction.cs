using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [ActionProperties(
        "Write Assembly Versions",
        "Updates AssemblyVersion, AssemblyFileVersion, and AssemblyInformationalVersion Attributes (in AssemblyInfo source files).",
        ".NET")]
    [CustomEditor(typeof(WriteAssemblyInfoVersionsActionEditor))]
    [RequiresInterface(typeof(IFileOperationsExecuter))]
    public sealed class WriteAssemblyInfoVersionsAction : AgentBasedActionBase
    {
        private static readonly Regex AttributeRegex = new Regex(@"(?<s>(System\.Reflection\.)?Assembly(File|Informational)?Version(Attribute)?\s*\(\s*"")[^""]*(?<e>""\s*\))", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteAssemblyInfoVersionsAction"/> class.
        /// </summary>
        public WriteAssemblyInfoVersionsAction()
        {
            this.FileMasks = new[] { "*\\AssemblyInfo.cs" };
            this.Version = "%RELNO%.%BLDNO%";
        }

        [Persistent]
        public string[] FileMasks { get; set; }
        [Persistent]
        public bool Recursive { get; set; }
        [Persistent]
        public string Version { get; set; }

        public override string ToString()
        {
            return string.Format(
                "Set Assembly Version Attributes in files matching ({0}) in {1} to {2}",
                string.Join(", ", this.FileMasks ?? new string[0]),
                Util.CoalesceStr(this.OverriddenSourceDirectory, "(default directory)"),
                this.Version
            );
        }

        protected override void Execute()
        {
            try
            {
                new Version(this.Version);
            }
            catch
            {
                this.LogError("The specified version ({0}) is not a valid .NET assembly version.", this.Version);
                return;
            }

            this.LogInformation("Setting Assembly Version Attributes to {0}...", this.Version);

            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();
            var entry = fileOps.GetDirectoryEntry(
                new GetDirectoryEntryCommand
                {
                    Path = this.Context.SourceDirectory,
                    IncludeRootPath = true,
                    Recurse = this.Recursive
                }
            ).Entry;

            var matches = Util.Files.Comparison.GetMatches(
                this.Context.SourceDirectory,
                entry,
                this.FileMasks
            ).OfType<FileEntryInfo>();

            if (!matches.Any())
            {
                this.LogWarning("No matching files found.");
                return;
            }

            var replacementText = "${s}" + this.Version + "${e}";

            foreach (var match in matches)
            {
                this.LogDebug("Writing assembly versions attributes to {0}...", match.Path);

                var text = Encoding.UTF8.GetString(fileOps.ReadFileBytes(match.Path));
                if (AttributeRegex.IsMatch(text))
                {
                    text = AttributeRegex.Replace(text, replacementText);
                    fileOps.WriteFileBytes(
                        match.Path,
                        Encoding.UTF8.GetBytes(text)
                    );
                }
            }
        }
    }
}
