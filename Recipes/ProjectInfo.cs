using System;
using System.Collections.Generic;
using System.Text;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    [Serializable]
    internal sealed class ProjectInfo
    {
        private char directorySeparator;
        private string scmPath;

        public ProjectInfo(char directorySeparator, string scmPath)
        {
            this.ConfigFiles = new List<string>();
            this.directorySeparator = directorySeparator;
            this.scmPath = scmPath;
        }

        public string ScmPath
        {
            get { return this.scmPath; }
        }
        public string ScmDirectoryName
        {
            get
            {
                int index = this.ScmPath.LastIndexOf('/');
                if (index >= 0)
                    return this.ScmPath.Substring(0, index);
                else
                    return string.Empty;
            }
        }
        public string FileSystemPath
        {
            get { return this.scmPath.Replace(this.directorySeparator, '\\'); }
        }
        public string ProjectFileName
        {
            get
            {
                return this.scmPath.Substring(this.scmPath.LastIndexOf(this.directorySeparator) + 1);
            }
        }
        public string Name
        {
            get
            {
                var fileName = this.ProjectFileName;
                int index = fileName.LastIndexOf('.');
                if (index >= 0)
                    return fileName.Substring(0, index);
                else
                    return fileName;
            }
        }
        public bool IsWebApplication { get; set; }
        public List<string> ConfigFiles { get; private set; }
        public string DeploymentTarget { get; set; }
    }
}
