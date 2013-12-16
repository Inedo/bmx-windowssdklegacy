using System.Collections.Generic;
using System.IO;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Action which generates a licenses.licx file and adds to project files.
    /// </summary>
    [ActionProperties(
        "Add Component License",
        "Generates a licenses.licx file and adds it to project files.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(AddComponentLicenseActionEditor))]
    public sealed class AddComponentLicenseAction : RemoteActionBase
    {
        /// <summary>
        /// Namespace URI for MSBuild project files.
        /// </summary>
        private const string NamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        /// Initializes a new instance of the <see cref="AddComponentLicenseAction"/> class.
        /// </summary>
        public AddComponentLicenseAction()
        {
            this.SearchMasks = new[] { "*.csproj", "*.vbproj" };
        }

        /// <summary>
        /// Gets or sets the search mask used to identify project files which should be licensed.
        /// </summary>
        [Persistent]
        public string[] SearchMasks { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the action should search for project files recursively.
        /// </summary>
        [Persistent]
        public bool Recursive { get; set; }
        /// <summary>
        /// Gets or sets the names of licensed components to add to the licenses.licx file.
        /// </summary>
        [Persistent]
        public string[] LicenesedComponents { get; set; }

        /// <summary>
        /// Returns a value indicating whether the action uses one or more settings in its
        /// extension configurer.
        /// </summary>
        /// <returns>
        /// True if the action uses at least one configurer settings; otherwise false.
        /// </returns>
        public override bool HasConfigurerSettings()
        {
            return false;
        }
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var masks = this.SearchMasks ?? new string[0];
            var components = this.LicenesedComponents ?? new string[0];

            return string.Format(
                "Add licenses ({0}) to projects matching ({1}) in {2}",
                string.Join("; ", components),
                string.Join("; ", masks),
                Util.CoalesceStr(this.OverriddenSourceDirectory, "(default directory)"));
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

        /// <summary>
        /// Adds licenses to a project.
        /// </summary>
        /// <param name="projectFile">The project file to add the licenses to.</param>
        private void AddToProject(string projectFile)
        {
            #region Add to Project File
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
            #endregion

            #region Generate licenses.licx Files
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
            #endregion
        }
    }
}
