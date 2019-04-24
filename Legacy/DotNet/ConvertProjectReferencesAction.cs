using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Files;
using Inedo.Documentation;
using Inedo.IO;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [DisplayName("Convert Project References")]
    [Description("Converts project references in .NET projects to file references.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(ConvertProjectReferencesActionEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.WindowsSdk.DotNet.ConvertProjectReferencesAction,WindowsSdk")]
    public sealed class ConvertProjectReferencesAction : RemoteActionBase
    {
        private const string NamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        [Persistent]
        public string LibraryPath { get; set; }

        [Persistent]
        public string[] SearchMasks { get; set; }

        [Persistent]
        public bool Recursive { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Change Project References to Assembly References"
                ),
                new RichDescription(
                    "in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory),
                    " matching ",
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
                this.LogError("Library path not specified.");
                return;
            }

            if (this.SearchMasks == null || this.SearchMasks.Length == 0)
            {
                this.LogError("Search mask not specified.");
                return;
            }

            this.LogDebug("Converting references...");
            this.ExecuteRemoteCommand("convert");

            this.LogInformation("Project file conversion complete.");
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            this.LibraryPath = PathEx.Combine(this.Context.SourceDirectory, this.LibraryPath);
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
                this.ConvertProject(projectFile.Path);

            return string.Empty;
        }

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

        private static string RelativePathTo(string from, string to)
        {
            Uri uriFrom = new Uri(from);
            Uri uriTo = new Uri(to);
            return uriFrom.MakeRelativeUri(uriTo).ToString().Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
