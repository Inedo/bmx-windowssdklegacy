using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Inedo.Agents;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.Documentation;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Operations.DotNet
{
    [Tag(Tags.DotNet)]
    [DisplayName("Write Assembly Versions")]
    [ScriptAlias("Write-AssemblyVersion")]
    [Description("Updates AssemblyVersion, AssemblyFileVersion, and AssemblyInformationalVersion Attributes (in AssemblyInfo source files).")]
    [ScriptNamespace("DotNet", PreferUnqualified = true)]
    public sealed class WriteAssemblyInfoVersionsOperation : ExecuteOperation
    {
        internal static readonly LazyRegex AttributeRegex = new LazyRegex(@"(?<1>(System\.Reflection\.)?Assembly(File|Informational)?Version(Attribute)?\s*\(\s*"")[^""]*(?<2>""\s*\))", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        
        [ScriptAlias("Version")]
        [DefaultValue("$ReleaseNumber.$PackageNumber")]
        public string Version { get; set; }
        [ScriptAlias("FromDirectory")]
        [DisplayName("From directory")]
        [PlaceholderText("$WorkingDirectory")]
        public string SourceDirectory { get; set; }

        [Category("Advanced")]
        [ScriptAlias("Include")]
        [Description(CommonDescriptions.MaskingHelp)]
        [DefaultValue("**\\AssemblyInfo.cs")]
        public IEnumerable<string> Includes { get; set; }
        [Category("Advanced")]
        [ScriptAlias("Exclude")]
        public IEnumerable<string> Excludes { get; set; }

        public override async Task ExecuteAsync(IOperationExecutionContext context)
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

            this.LogInformation("Setting assembly version attributes to {0}...", this.Version);

            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var matches = (await fileOps.GetFileSystemInfosAsync(context.ResolvePath(this.SourceDirectory), new MaskingContext(this.Includes, this.Excludes)).ConfigureAwait(false))
                .OfType<SlimFileInfo>()
                .ToList();

            if (matches.Count == 0)
            {
                this.LogWarning("No matching files found.");
                return;
            }

            var replacementText = "${1}" + this.Version + "${2}";

            foreach (var match in matches)
            {
                this.LogInformation("Writing assembly versions attributes to {0}...", match.FullName);
                string text;
                Encoding encoding;

                using (var stream = await fileOps.OpenFileAsync(match.FullName, FileMode.Open, FileAccess.Read).ConfigureAwait(false))
                using (var reader = new StreamReader(stream, true))
                {
                    text = await reader.ReadToEndAsync().ConfigureAwait(false);
                    encoding = reader.CurrentEncoding;
                }

                if (AttributeRegex.IsMatch(text))
                {
                    text = AttributeRegex.Replace(text, replacementText);

                    var attr = match.Attributes;
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        await fileOps.SetAttributesAsync(match.FullName, attr & ~FileAttributes.ReadOnly).ConfigureAwait(false);

                    using (var stream = await fileOps.OpenFileAsync(match.FullName, FileMode.Create, FileAccess.Write).ConfigureAwait(false))
                    using (var writer = new StreamWriter(stream, encoding))
                    {
                        await writer.WriteAsync(text).ConfigureAwait(false);
                    }
                }
            }
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Set AssemblyVersion Attributes to ",
                    new Hilite(config[nameof(Version)])
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(config[nameof(SourceDirectory)]),
                    " matching ",
                    new MaskHilite(config[nameof(Includes)], config[nameof(Excludes)])
                )
            );
        }
    }
}
