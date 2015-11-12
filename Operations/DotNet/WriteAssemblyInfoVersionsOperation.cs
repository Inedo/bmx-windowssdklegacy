using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Extensibility.Operations;
using Inedo.Diagnostics;
using Inedo.IO;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Operations.DotNet
{
    [Tag(Tags.DotNet)]
    [DisplayName("Write Assembly Versions")]
    [ScriptAlias("Write-AssemblyVersion")]
    [Description("Updates AssemblyVersion, AssemblyFileVersion, and AssemblyInformationalVersion Attributes (in AssemblyInfo source files).")]
    public sealed class WriteAssemblyInfoVersionsOperation : ExecuteOperation
    {
        internal static readonly LazyRegex AttributeRegex = new LazyRegex(@"(?<1>(System\.Reflection\.)?Assembly(File|Informational)?Version(Attribute)?\s*\(\s*"")[^""]*(?<2>""\s*\))", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        [Required]
        [ScriptAlias("Include")]
        [Description(CommonDescriptions.IncludeMask)]
        [DefaultValue("**\\AssemblyInfo.cs")]
        public IEnumerable<string> Includes { get; set; }
        [ScriptAlias("Exclude")]
        [Description(CommonDescriptions.ExcludeMask)]
        public IEnumerable<string> Excludes { get; set; }
        [Required]
        [ScriptAlias("Version")]
        [DefaultValue("$ReleaseNumber.$BuildNumber")]
        public string Version { get; set; }
        [ScriptAlias("FromDirectory")]
        [Description(CommonDescriptions.SourceDirectory)]
        public string SourceDirectory { get; set; }

        public override Task ExecuteAsync(IOperationExecutionContext context)
        {
            try
            {
                new Version(this.Version);
            }
            catch
            {
                this.LogError("The specified version ({0}) is not a valid .NET assembly version.", this.Version);
                return Complete;
            }

            this.LogInformation("Setting assembly version attributes to {0}...", this.Version);

            var fileOps = context.Agent.GetService<IFileOperationsExecuter>();
            var matches = fileOps.GetFileSystemInfos(this.SourceDirectory ?? context.WorkingDirectory, new MaskingContext(this.Includes, this.Excludes))
                .OfType<SlimFileInfo>()
                .ToList();

            if (matches.Count == 0)
            {
                this.LogWarning("No matching files found.");
                return Complete;
            }

            var replacementText = "${1}" + this.Version + "${2}";

            foreach (var match in matches)
            {
                this.LogInformation("Writing assembly versions attributes to {0}...", match.FullName);
                string text;
                Encoding encoding;

                using (var stream = fileOps.OpenFile(match.FullName, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(stream, true))
                {
                    text = reader.ReadToEnd();
                    encoding = reader.CurrentEncoding;
                }

                if (AttributeRegex.IsMatch(text))
                {
                    text = AttributeRegex.Replace(text, replacementText);

                    var attr = match.Attributes;
                    if ((attr & FileAttributes.ReadOnly) != 0)
                        fileOps.SetAttributes(match.FullName, null, attr & ~FileAttributes.ReadOnly);

                    using (var stream = fileOps.OpenFile(match.FullName, FileMode.Create, FileAccess.Write))
                    using (var writer = new StreamWriter(stream, encoding))
                    {
                        writer.Write(text);
                    }
                }
            }

            return Complete;
        }

        protected override ExtendedRichDescription GetDescription(IOperationConfiguration config)
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Set AssemblyVersion Attributes to "
                    //this.Version
                ),
                new RichDescription(
                    "in ",
                    //this.OverriddenSourceDirectory,
                    " matching "
                    //new ListHilite(this.FileMasks)
                )
            );
        }
    }
}
