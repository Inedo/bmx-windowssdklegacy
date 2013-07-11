using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    [Serializable]
    internal sealed class MSBuildProject
    {
        private const string NamespaceUri = "http://schemas.microsoft.com/developer/msbuild/2003";

        private List<ReferencedAssembly> references = new List<ReferencedAssembly>();
        private List<ReferencedProject> referencedProjects = new List<ReferencedProject>();
        private List<ProjectFile> projectFiles = new List<ProjectFile>();

        private MSBuildProject()
        {
        }

        public IList<ReferencedAssembly> References
        {
            get { return this.references; }
        }
        public IList<ReferencedProject> ReferencedProjects
        {
            get { return this.referencedProjects; }
        }
        public IList<ProjectFile> Files
        {
            get { return this.projectFiles; }
        }
        public bool IsWebApplication { get; private set; }

        static void Main(string[] args)
        {
            var solution = Solution.Load(@"C:\Projects\ProGet\ProGet.sln");
            var project = Load(@"C:\Projects\ProGet\ProGet.WebApplication\ProGet.WebApplication.csproj");
        }

        public static MSBuildProject Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("fileName");

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(fileName);
            return Load(xmlDocument);
        }
        public static MSBuildProject Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(stream);
            return Load(xmlDocument);
        }
        public static MSBuildProject Load(XmlDocument xmlDocument)
        {
            if (xmlDocument == null)
                throw new ArgumentNullException("xmlDocument");

            var nsManager = new XmlNamespaceManager(xmlDocument.NameTable);
            nsManager.AddNamespace("m", NamespaceUri);

            var project = new MSBuildProject();

            foreach (XmlElement itemElement in xmlDocument.SelectNodes("//m:ItemGroup/*[@Include]", nsManager))
            {
                if (itemElement.LocalName == "Reference")
                {
                    var hintPath = (XmlElement)itemElement.SelectSingleNode("m:HintPath", nsManager);
                    if (hintPath != null)
                        project.references.Add(new ReferencedAssembly(itemElement.GetAttribute("Include"), hintPath.InnerText));
                    else
                        project.references.Add(new ReferencedAssembly(itemElement.GetAttribute("Include"), null));
                }
                else if (itemElement.LocalName == "ProjectReference")
                {
                    var projectName = (XmlElement)itemElement.SelectSingleNode("m:Name", nsManager);
                    if (projectName != null)
                        project.referencedProjects.Add(new ReferencedProject(projectName.InnerText, itemElement.GetAttribute("Include")));
                }
                else
                {
                    project.projectFiles.Add(new ProjectFile(itemElement.GetAttribute("Include"), itemElement.LocalName));
                }
            }

            foreach(XmlAttribute attribute in xmlDocument.SelectNodes("//m:Import/@Project", nsManager))
            {
                if(attribute.Value != null && attribute.Value.IndexOf("WebApplication", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    project.IsWebApplication = true;
                    break;
                }
            }

            return project;
        }
    }

    [Serializable]
    internal sealed class ProjectFile
    {
        public ProjectFile(string name, string buildAction)
        {
            this.Name = name;
            this.BuildAction = buildAction;
        }

        public string Name { get; private set; }
        public string BuildAction { get; private set; }

        public override string ToString()
        {
            return this.BuildAction + " " + this.Name;
        }
    }

    [Serializable]
    internal sealed class ReferencedAssembly
    {
        public ReferencedAssembly(string name, string hintPath)
        {
            this.Name = name;
            this.HintPath = hintPath;
        }

        public string Name { get; private set; }
        public string HintPath { get; private set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    [Serializable]
    internal sealed class ReferencedProject
    {
        public ReferencedProject(string name, string includePath)
        {
            this.Name = name;
            this.IncludePath = includePath;
        }

        public string Name { get; private set; }
        public string IncludePath { get; private set; }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
