using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [DisplayName("Add Component License")]
    [Description("Generates a licenses.licx file and adds it to project files.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(AddComponentLicenseActionEditor))]
    public sealed class AddComponentLicenseAction : RemoteActionBase
    {
        private const string NamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        public AddComponentLicenseAction()
        {
            this.SearchMasks = new[] { "*.csproj", "*.vbproj" };
        }

        [Persistent]
        public string[] SearchMasks { get; set; }
        [Persistent]
        public bool Recursive { get; set; }
        [Persistent]
        public string[] LicenesedComponents { get; set; }

        public override bool HasConfigurerSettings()
        {
            return false;
        }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Add Licenses to Projects in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory)
                ),
                new RichDescription(
                    "for ",
                    new ListHilite(this.LicenesedComponents)
                )
            );
        }

        protected override void Execute()
        {
            if (this.SearchMasks == null || this.SearchMasks.Length == 0)
            {
                LogInformation("No search masks provided. Nothing to do.");
                return;
            }

            if (this.LicenesedComponents == null || this.LicenesedComponents.Length == 0)
            {
                LogInformation("No licensed components specified. Nothing to do.");
                return;
            }

            ExecuteRemoteCommand("add");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var entryInfo = Util.Files.GetDirectoryEntry(new GetDirectoryEntryCommand
            {
                Path = this.Context.SourceDirectory,
                IncludeRootPath = true,
                Recurse = this.Recursive
            });

            var matches = Util.Files.Comparison.GetMatches(this.Context.SourceDirectory, entryInfo.Entry, this.SearchMasks);
            foreach (var match in matches)
            {
                if (!(match is FileEntryInfo))
                    continue;

                LogInformation(string.Format("Adding licenses to {0}...", match.Path));
                AddToProject(match.Path);
            }

            return string.Empty;
        }

        private void AddToProject(string projectFile)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile);
            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("d", NamespaceUri);

            if (xmlDoc.SelectSingleNode("/d:Project/d:ItemGroup/d:EmbeddedResource[Include=\"licenses.licx\"]", nsManager) != null)
            {
                // Project already has a licenses.licx file.
                LogDebug("Project already has a licenses.licx file.");
            }
            else
            {
                var projectNode = xmlDoc.SelectSingleNode("/d:Project", nsManager) as XmlElement;
                if (projectNode == null)
                    throw new InvalidDataException(string.Format("{0} is not a valid project file.", projectFile));

                var itemGroup = xmlDoc.CreateElement("ItemGroup", NamespaceUri);
                var embeddedResource = xmlDoc.CreateElement("EmbeddedResource", NamespaceUri);
                embeddedResource.SetAttribute("Include", "licenses.licx");
                itemGroup.AppendChild(embeddedResource);
                projectNode.AppendChild(itemGroup);

                // Unset read-only if necessary.
                var fileAttr = File.GetAttributes(projectFile);
                if ((fileAttr & FileAttributes.ReadOnly) != 0)
                    File.SetAttributes(projectFile, fileAttr & ~FileAttributes.ReadOnly);

                xmlDoc.Save(projectFile);
            }

            var licensesToAdd = new List<string>(this.LicenesedComponents);

            var licensesPath = Path.Combine(Path.GetDirectoryName(projectFile), "licenses.licx");
            if (File.Exists(licensesPath))
            {
                foreach (var license in File.ReadAllLines(licensesPath))
                    licensesToAdd.Remove(license);
            }

            if (licensesToAdd.Count == 0)
                return;

            using (var licenseStream = new StreamWriter(licensesPath, true))
            {
                foreach (var license in licensesToAdd)
                    licenseStream.WriteLine(license);
            }
        }
    }
}
