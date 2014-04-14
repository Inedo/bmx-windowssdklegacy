using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Files;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Defines an action for converting .NET project references to file references.
    /// </summary>
    [ActionProperties(
        "Convert Project References",
        "Converts project references in .NET projects to file references.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(ConvertProjectReferencesActionEditor))]
    public sealed class ConvertProjectReferencesAction : RemoteActionBase
    {
        /// <summary>
        /// Namespace URI for MSBuild project files.
        /// </summary>
        private const string NamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        /// <summary>
        /// Initializes a new instance of the ConvertProjectLibraryAction class.
        /// </summary>
        public ConvertProjectReferencesAction()
        {
        }

        /// <summary>
        /// Gets or sets the path to the library directory.
        /// </summary>
        [Persistent]
        public string LibraryPath { get; set; }

        /// <summary>
        /// Gets or sets the search mask used to identify project files to convert.
        /// </summary>
        [Persistent]
        public string[] SearchMasks { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the action should search for project files recursively.
        /// </summary>
        [Persistent]
        public bool Recursive { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "Change Project References to Assembly References"
                ),
                new LongActionDescription(
                    "in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory),
                    " and matching ",
                    new ListHilite(this.SearchMasks),
                    " with library path ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.LibraryPath)
                )
            );
        }

        public override bool HasConfigurerSettings()
        {
            return false;
        }

        protected override void Execute()
        {
            if (string.IsNullOrEmpty(this.LibraryPath))
            {
                LogError("Library path not specified.");
                return;
            }

            if (SearchMasks == null || SearchMasks.Length == 0)
            {
                LogError("Search mask not specified.");
                return;
            }

            LogDebug("Converting references...");
            ExecuteRemoteCommand("convert");

            LogInformation("Project file conversion complete.");
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            this.LibraryPath = Util.Path2.Combine(this.Context.SourceDirectory, this.LibraryPath);
            var sourcePath = this.Context.SourceDirectory;
            LogDebug("Getting entries at " + sourcePath + "...");

            var entry = Util.Files.GetDirectoryEntry(
                new GetDirectoryEntryCommand
                {
                    Path = sourcePath,
                    IncludeRootPath = true,
                    Recurse = this.Recursive
                }
            ).Entry;

            var matches = Util.Files.Comparison.GetMatches(sourcePath, entry, this.SearchMasks);

            foreach (var projectFile in matches)
                ConvertProject(projectFile.Path);

            return string.Empty;
        }

        /// <summary>
        /// Converts an MSBuild project file's project references to file references.
        /// </summary>
        /// <param name="projectFile">Full path to project file to convert.</param>
        private void ConvertProject(string projectFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(projectFile);

            var currentPath = Path.GetDirectoryName(projectFile);

            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("d", NamespaceUri);

            // Find all of the project references.
            var projectRefNodes = xmlDoc.SelectNodes("/d:Project/d:ItemGroup/d:ProjectReference", nsManager);
            if (projectRefNodes == null || projectRefNodes.Count == 0)
            {
                LogDebug(string.Format("Nothing to convert in '{0}'", Path.GetFileName(projectFile)));
                return;
            }

            var nodesToReplace = new List<XmlNode>(projectRefNodes.Count);
            foreach (XmlNode node in projectRefNodes)
                nodesToReplace.Add(node);

            LogInformation("Converting {0} reference(s) in '{1}'", nodesToReplace.Count, Path.GetFileName(projectFile));

            foreach (var projectRefNode in nodesToReplace)
            {
                // Try to determine the actual assembly file being referenced.
                var refProjectFile = Path.GetFullPath(Path.Combine(currentPath, projectRefNode.Attributes["Include"].Value));
                LogDebug("Loading '" + refProjectFile + "' for referenced assembly name...");
                var refAsmName = GetReferencedAssemblyName(refProjectFile);
                if (string.IsNullOrEmpty(refAsmName))
                {
                    refAsmName = Path.GetFileNameWithoutExtension(refProjectFile);
                    LogWarning("Could not determine referenced assembly name from project {0}; using {1}.dll", refProjectFile, refAsmName);
                }

                var privateNode = projectRefNode.SelectSingleNode("d:Private", nsManager) as XmlElement;
                if (privateNode != null) privateNode = privateNode.CloneNode(true) as XmlElement;

                var assemblyPath = Path.Combine(this.LibraryPath, refAsmName + ".dll");
                try
                {
                    // Try to load the assembly to verify that it is valid and to get its full name.
                    var assemblyFullName = GetFullAssemblyName(assemblyPath);
                    var newNode = CreateFileReference(assemblyFullName.FullName, projectFile, assemblyPath, privateNode, xmlDoc);

                    // Replace the project node with the file node.
                    projectRefNode.ParentNode.ReplaceChild(newNode, projectRefNode);
                }
                catch (FileNotFoundException)
                {
                    LogWarning(string.Format("Could not find referenced assembly '{0}'", assemblyPath));
                    continue;
                }
                catch (DirectoryNotFoundException)
                {
                    LogWarning(string.Format("Could not find referenced assembly '{0}'", assemblyPath));
                    continue;
                }
            }

            var attribs = File.GetAttributes(projectFile);
            File.SetAttributes(projectFile, attribs & ~FileAttributes.ReadOnly);
            xmlDoc.Save(projectFile);
            File.SetAttributes(projectFile, attribs);
        }

        /// <summary>
        /// Returns the full name of an assembly.
        /// </summary>
        /// <param name="assemblyFileName">The file name of the assembly.</param>
        /// <returns>The full name of the assembly.</returns>
        private static AssemblyName GetFullAssemblyName(string assemblyFileName)
        {
            try
            {
                return AssemblyName.GetAssemblyName(assemblyFileName);
            }
            catch (BadImageFormatException ex)
            {
                throw new BadImageFormatException(string.Format("Could not extract full name for {0}. It may not be a valid assembly.", assemblyFileName), ex);
            }
        }

        /// <summary>
        /// Returns the target assembly name read from a project file.
        /// </summary>
        /// <param name="projectFile">MSBuild project file to read.</param>
        /// <returns>Name of the target assembly read from the project file if found; otherwise null.</returns>
        private static string GetReferencedAssemblyName(string projectFile)
        {
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(projectFile);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }

            var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
            nsManager.AddNamespace("d", NamespaceUri);

            var node = xmlDoc.SelectSingleNode("/d:Project/d:PropertyGroup/d:AssemblyName", nsManager);
            if (node != null)
                return node.InnerText;
            else
                return null;
        }

        /// <summary>
        /// Creates a new .NET assembly reference as an XML element.
        /// </summary>
        /// <param name="assemblyFullName">Full name of the assembly to reference.</param>
        /// <param name="projectPath">Full path to the project file which contains the reference.</param>
        /// <param name="libraryPath">Full path to the library assembly to reference.</param>
        /// <param name="xmlDoc">XmlDocument instance used to create a new node.</param>
        /// <returns>XmlElement specifying the MSBuild assembly file reference.</returns>
        private static XmlNode CreateFileReference(string assemblyFullName, string projectPath, string libraryPath, XmlElement privateNode, XmlDocument xmlDoc)
        {
            var newNode = xmlDoc.CreateElement("Reference", NamespaceUri);
            newNode.SetAttribute("Include", assemblyFullName);

            var specificVersion = xmlDoc.CreateElement("SpecificVersion", NamespaceUri);
            specificVersion.InnerText = "False";
            newNode.AppendChild(specificVersion);

            var hintPath = xmlDoc.CreateElement("HintPath", NamespaceUri);
            hintPath.InnerText = RelativePathTo(projectPath, libraryPath);
            newNode.AppendChild(hintPath);

            if (privateNode != null) newNode.AppendChild(privateNode);

            return newNode;
        }

        /// <summary>
        /// Attempts to determine a relative path between two absolute paths.
        /// </summary>
        /// <param name="from">Starting absolute path.</param>
        /// <param name="to">Destination absolute path.</param>
        /// <returns>Relative path from the start to the destination.</returns>
        private static string RelativePathTo(string from, string to)
        {
            Uri uriFrom = new Uri(from);
            Uri uriTo = new Uri(to);
            return uriFrom.MakeRelativeUri(uriTo).ToString().Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
