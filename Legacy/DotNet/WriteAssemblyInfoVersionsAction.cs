using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Documentation;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [DisplayName("Write Assembly Versions")]
    [Description("Updates AssemblyVersion, AssemblyFileVersion, and AssemblyInformationalVersion Attributes (in AssemblyInfo source files).")]
    [Tag(Tags.DotNet)]
    [Inedo.Web.CustomEditor(typeof(WriteAssemblyInfoVersionsActionEditor))]
    public sealed class WriteAssemblyInfoVersionsAction : AgentBasedActionBase
    {
        private static readonly LazyRegex AttributeRegex = new LazyRegex(@"(?<1>(System\.Reflection\.)?Assembly(File|Informational)?Version(Attribute)?\s*\(\s*"")[^""]*(?<2>""\s*\))", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        [Persistent]
        public string[] FileMasks { get; set; }
        [Persistent]
        public bool Recursive { get; set; }
        [Persistent]
        public string Version { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Set AssemblyVersion Attributes to ",
                    new Hilite(this.Version)
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory),
                    " matching ",
                    new ListHilite(this.FileMasks)
                )
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
                this.LogError($"The specified version ({this.Version}) is not a valid .NET assembly version.");
                return;
            }

            this.LogInformation($"Setting Assembly Version Attributes to {this.Version}...");

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

            var replacementText = "${1}" + this.Version + "${2}";

            foreach (var match in matches)
            {
                this.LogInformation($"Writing assembly versions attributes to {match.Path}...");
                string text;
                Encoding encoding;

                using (var stream = fileOps.OpenFile(match.Path, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream, true))
                {
                    text = reader.ReadToEnd();
                    encoding = reader.CurrentEncoding;
                }

                if (AttributeRegex.IsMatch(text))
                {
                    text = AttributeRegex.Replace(text, replacementText);

                    var attr = fileOps.GetFileInfo(match.Path).Attributes;
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        fileOps.SetAttributesAsync(match.Path, attr & ~FileAttributes.ReadOnly).Wait();

                    using (var stream = fileOps.OpenFile(match.Path, FileMode.Create, FileAccess.Write))
                    using (var writer = new StreamWriter(stream, encoding))
                    {
                        writer.Write(text);
                    }
                }
            }
        }
    }
}
