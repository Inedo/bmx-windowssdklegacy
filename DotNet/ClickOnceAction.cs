using System;
using System.IO;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Represents an action that prepares a ClickOnce application for deployment
    /// </summary>
    [ActionProperties(
        "Prepare ClickOnce Application",
        "Prepares a ClickOnce application for deployment.",
        ".NET")]
    [CustomEditor(typeof(ClickOnceActionEditor))]
    public sealed class ClickOnceAction : RemoteActionBase
    {
        /// <summary>
        /// Gets or sets the name of the application whose manifest is being generated or updated. E.g. MyWpfApplication.
        /// </summary>
        [Persistent]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Gets or sets the provider URL to be use in the deployment manifest being 
        /// generated or updated. E.g. http://apps.example.com/MyWpfApplication
        /// </summary>
        [Persistent]
        public string ProviderUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of an X509 certificate file with which to sign a
        /// manifest or license file.  This option requires the -Password option
        /// </summary>
        [Persistent]
        public string CertificatePath { get; set; }

        /// <summary>
        /// Gets or sets the password to use with an X509 certificate when signing
        /// a manifest or license file. 
        /// </summary>
        [Persistent]
        public string CertificatePassword { get; set; }

        /// <summary>
        /// Gets or sets the hash of an X509 certificate in the local cert store.
        /// </summary>
        [Persistent]
        public string CertificateHash { get; set; }

        /// <summary>
        /// Gets or sets the version of the application whose manifest is being
        /// generated or updated.  Must be of the form "0.0.0.0".
        /// </summary>
        [Persistent]
        public string Version { get; set; }

        /// <summary>
        /// Determines whether files in the deployment will have a .deploy extension. 
        /// ClickOnce will strip this extension off these files as soon as it downloads them 
        /// from the Web server. This parameter allows all the files within a ClickOnce deployment 
        /// to be downloaded from a Web server that blocks transmission of files ending in "unsafe" 
        /// extensions such as .exe. 
        /// </summary>
        [Persistent]
        public bool MapFileExtensions { get; set; }

        /// <summary>
        /// Indicates whether or not the ClickOnce application should install onto the local machine, 
        /// or whether it should run from the Web. Installing an application gives that application a 
        /// presence in the Windows Start menu. Valid values are "true" or "t", and "false" or "f". 
        /// If unspecified, the default value is “true”.
        /// </summary>
        [Persistent]
        public bool InstallApplication { get; set; }

        protected override void Execute()
        {
            LogDebug("Creating Application Manifest File...");
            ExecuteRemoteCommand("CreateApplication");
            LogInformation("Application Manifest File created.");

            LogDebug("Signing Application Manifest File...");
            ExecuteRemoteCommand("SignApplication");
            LogInformation("Application Manifest File signed.");

            LogDebug("Creating Deployment Manifest File...");
            ExecuteRemoteCommand("CreateDeployment");
            LogInformation("Deployment Manifest File created.");

            LogDebug("Signing Deployment Manifest File...");
            ExecuteRemoteCommand("SignDeployment");
            LogInformation("Deployment Manifest File signed.");

            LogDebug("Copying application files...");
            ExecuteRemoteCommand("CopyAppFiles");
            LogInformation(
                "Application files copied "
                + (MapFileExtensions ? "and mapped to .deploy" :"")
                + ".");
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            string appManifest = Path.Combine(
                Context.TargetDirectory,
                ApplicationName + ".exe.manifest");
            string deployManifest = Path.Combine(
                Context.TargetDirectory,
                ApplicationName + ".application");


            if (name == "CreateApplication")
            {
                //%_MAGE% -New Application -ToFile %_PRJNME%.exe.manifest -Version %1 -FromDirectory .
                return ExecuteCommandLine(
                    GetMagePath(),
                    string.Format(
                        "-New Application "
                        + "-ToFile \"{0}\" "
                        + "-Version {1} "
                        + "-FromDirectory .",
                        appManifest,
                        Version),
                    Context.SourceDirectory).ToString();
            }
            else if (name == "SignApplication")
            {
                //%_MAGE% -Sign %_PRJNME%.exe.manifest -CertFile %_CERT%
                return ExecuteCommandLine(
                    GetMagePath(),
                    string.Format(
                        "-Sign \"{0}\" {1}",
                        appManifest,
                        GetCertificateArguments()),
                    Context.SourceDirectory).ToString();
            }
            else if (name == "CreateDeployment")
            { 
                //%_MAGE% -New Deployment -ToFile %_PRJNME%.application -Version %1 -AppManifest %_PRJNME%.exe.manifest -providerUrl %_PROVURL%/%_PRJNME%.application
                ExecuteCommandLine(
                    GetMagePath(),
                    string.Format(
                        "-New Deployment "
                        + "-ToFile \"{0}\" "
                        + "-Version {1} "
                        + "-AppManifest \"{2}\" "
                        + "-providerUrl \"{3}/{4}.application\" "
                        +" -Install {5} ",
                        deployManifest,
                        Version,
                        appManifest,
                        ProviderUrl,
                        ApplicationName,
                        InstallApplication.ToString().ToLower()),
                    Context.SourceDirectory).ToString();

                if (MapFileExtensions)
                {
                    LogDebug("Adding mapFileExtensions attribute...");

                    // Load the deployment Manifest
                    var deployXml = new XmlDocument();
                    deployXml.Load(deployManifest);

                    // Find the /assembly/deployment node
                    XmlElement deploymentElement = null;
                    foreach (XmlNode child in deployXml.DocumentElement.ChildNodes)
                        if (child.Name == "deployment") deploymentElement = (XmlElement)child;
                    if (deploymentElement == null) { LogError("deployment element not found"); return null; }
                    deploymentElement.SetAttribute("mapFileExtensions", "true");

                    deployXml.Save(deployManifest);


                    LogInformation("Attribute mapFileExtensions added.");
                }

                return null;
            }
            else if (name == "SignDeployment")
            {
                //%_MAGE% -Sign %_PRJNME%.application -CertFile %_CERT%
                return ExecuteCommandLine(
                    GetMagePath(),
                    string.Format(
                        "-Sign \"{0}\" {1}",
                        deployManifest,
                        GetCertificateArguments()),
                    Context.SourceDirectory).ToString();
            }
            else if (name == "CopyAppFiles")
            {
                CopyFiles(
                    Context.SourceDirectory,
                    Context.TargetDirectory,
                    MapFileExtensions);
                return null;
            }
            else
            {
                throw new ArgumentOutOfRangeException("name");
            }           

        }

        private string GetCertificateArguments()
        {
            if (!string.IsNullOrEmpty(this.CertificateHash))
                return string.Format("-CertHash \"{0}\" ", this.CertificateHash);

            if (string.IsNullOrEmpty(this.CertificatePassword))
                return string.Format("-CertFile \"{0}\" ", this.CertificatePath);

            return string.Format("-CertFile \"{0}\" -Password \"{1}\" ", this.CertificatePath, this.CertificatePassword);
        }
        
        // A riff on Util.Files.CopyFiles, with the rename included
        //   Copy/Pasting code generally isn't a good idea, but this is a pretty rare function
        //   and recursing directories to copy is pretty straight forward
        void CopyFiles(string sourceFolder, string targetFolder, bool renameToDeploy)
        {
            // If the source path isn't found, there's nothing to copy
            if (!Directory.Exists(sourceFolder))
                return;

            // If the target path doesn't exist, create it
            if (!Directory.Exists(targetFolder))
                Directory.CreateDirectory(targetFolder);

            DirectoryInfo sourceFolderInfo = new DirectoryInfo(sourceFolder);
            // Copy each file
            foreach (FileInfo theFile in sourceFolderInfo.GetFiles())
            {
                string destFileName = Path.Combine(targetFolder, theFile.Name);
                if (renameToDeploy) destFileName += ".deploy";
                theFile.CopyTo(destFileName);
            }

            // Recurse subdirectories
            foreach (DirectoryInfo subfolder in sourceFolderInfo.GetDirectories())
            {
                CopyFiles(subfolder.FullName, Path.Combine(targetFolder, subfolder.Name), renameToDeploy);
            }
        }

        public override string ToString()
        {
            return string.Format(
                "Create ClickOnce Application ({0}) in {1}.",
                ApplicationName,  
		        (String.IsNullOrEmpty(OverriddenSourceDirectory)
			        ? "default directory"
			        : OverriddenSourceDirectory)
            );
        }

        private string GetMagePath()
        {
            var sdkPath = ((WindowsSdkExtensionConfigurer)GetExtensionConfigurer()).WindowsSdkPath;

            // HACK: if the NETFX 4.0 Tools directory exists, use it - otherwise just use bin\mage.exe
            string magePath40 = Path.Combine(sdkPath, @"bin\NETFX 4.0 Tools\mage.exe");
            if (File.Exists(magePath40)) return magePath40;

            return Path.Combine(sdkPath, @"bin\mage.exe");
        }
    }
}
