using System;
using System.IO;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using System.Reflection;
using System.Data.Common;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Represents an action that prepares a ClickOnce application for deployment
    /// </summary>
    [ActionProperties(
        "Prepare ClickOnce Application",
        "Prepares a ClickOnce application for deployment.")]
    [Tag(Tags.DotNet)]
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
        /// Gets or sets the the minimum version of the application that can run on the client. 
        /// Must be of the form "0.0.0.0".   If InstallApplication is false it will be ignored.
        /// </summary>
        [Persistent]
        public string MinVersion { get; set; }


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

        /// <summary>
        /// Gets or sets the option to create a desktop Icon for applications being installed onto the local machine
        /// along with the presence in the Windows Start menu.
        /// </summary>
        [Persistent]
        public bool CreateDesktopIcon { get; set; }

        /// <summary>
        /// Gets or sets the full path to an .ICO icon file to be used for the application Icon. This icon appears 
        /// beside your application name in the start menu, on the desktop if CreateDesktopIcon is set to true, 
        /// and in its Add-or-Remove Programs entry. If no icon is provided and CreateDesktopIcon is true, a default icon is used.
        /// </summary>
        [Persistent]
        public string IconFile { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether or not installed applications should check for updates before it runs.
        /// If an update is found the application will be updated and then opened.
        /// </summary>
        [Persistent]
        public bool StartupUpdateCheck { get; set; }

        [Persistent]
        public string EntryPointFile { get; set; }

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
                + (MapFileExtensions ? "and mapped to .deploy" : "")
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
                //%_MAGE% -New Application -ToFile %_PRJNME%.exe.manifest -Version %1 -FromDirectory 

                string icon = String.IsNullOrEmpty(IconFile) ? String.Empty : String.Format("-IconFile \"{0}\" ", IconFile);

                ExecuteCommandLine(
                    GetMagePath(),
                    string.Format(
                    "-New Application "
                    + "-ToFile \"{0}\" "
                    + "-Version {1} "
                    + "-FromDirectory . "
                    + "{2}",
                    appManifest,
                    Version,
                    icon),
                    Context.SourceDirectory).ToString();

                UpdateApplicationManifest(appManifest);
                return null;
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
                        + " -Install {5} ",
                        deployManifest,
                        Version,
                        appManifest,
                        ProviderUrl,
                        ApplicationName,
                        InstallApplication.ToString().ToLower()),
                    Context.SourceDirectory).ToString();


                if (InstallApplication && !String.IsNullOrEmpty(MinVersion))
                {
                    ExecuteCommandLine(
                        GetMagePath(),
                        string.Format(
                            "-Update "
                            + "\"{0}\" "
                            + "-MinVersion {1} ",
                            deployManifest,
                            MinVersion),
                        Context.SourceDirectory).ToString();
                }

                //Moved existing MapFileExtensions functionality to the function UpdateApplicationFile

                UpdateApplicationFile(deployManifest);
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

        private void UpdateApplicationManifest(string applicationManifest)
        {
            if (String.IsNullOrWhiteSpace(this.EntryPointFile)) return;

            XmlDocument doc = new XmlDocument();
            doc.Load(applicationManifest);

            XmlNamespaceManager nsmgr = CreateNamespaceManager(doc);

            XmlNode entryPointNode = doc.SelectSingleNode("asmv1:assembly/asmv2:entryPoint", nsmgr);

            var assemblyName = AssemblyName.GetAssemblyName(Path.Combine(this.Context.SourceDirectory, this.EntryPointFile));

            //var connectionStringBuilder = new DbConnectionStringBuilder();
            //connectionStringBuilder.ConnectionString = assemblyName.FullName;

            
            XmlElement assemblyIdentityNode = (XmlElement)entryPointNode.SelectSingleNode("asmv2:assemblyIdentity", nsmgr);
            assemblyIdentityNode.SetAttribute("name", assemblyName.Name);
            assemblyIdentityNode.SetAttribute("version", assemblyName.Version.ToString());

            var publicKeyToken = assemblyName.GetPublicKeyToken();

            const string publicKeyTokenAttributeName = "publicKeyToken";
            if (publicKeyToken.Length > 0)
            {
                assemblyIdentityNode.SetAttribute(publicKeyTokenAttributeName, BitConverter.ToString(publicKeyToken).Replace("-", string.Empty).ToUpper());
            }
            else if (assemblyIdentityNode.HasAttribute(publicKeyTokenAttributeName))
            {
                assemblyIdentityNode.RemoveAttribute(publicKeyTokenAttributeName);
            }
            var culture = assemblyName.CultureInfo.Name;
            assemblyIdentityNode.SetAttribute("language", String.IsNullOrWhiteSpace(culture) ? "neutral" : culture);
            assemblyIdentityNode.SetAttribute("processorArchitecture", assemblyName.ProcessorArchitecture.ToString().ToLower());

            


            XmlElement commandLineNode = (XmlElement)entryPointNode.SelectSingleNode("asmv2:commandLine", nsmgr);
            commandLineNode.SetAttribute("file", this.EntryPointFile);

            doc.Save(applicationManifest);
        }

        private void UpdateApplicationFile(string deployManifest)
        {
            //ClickOnce Deployment Manifest Reference: http://msdn.microsoft.com/en-us/library/k26e96zf.aspx
            XmlDocument doc = new XmlDocument();
            doc.Load(deployManifest);

            XmlNamespaceManager nsmgr = CreateNamespaceManager(doc);

            XmlNode deploymentNode = doc.SelectSingleNode("asmv1:assembly/asmv2:deployment", nsmgr);
            if (deploymentNode != null)
            {
                if (InstallApplication && CreateDesktopIcon)
                {
                    LogDebug("Adding CreateDesktopShortcut attribute...");
                    ((XmlElement)deploymentNode).SetAttribute("createDesktopShortcut", "urn:schemas-microsoft-com:clickonce.v1", "true");
                }

                if (StartupUpdateCheck)
                {
                    XmlNode updateNode = deploymentNode.SelectSingleNode("asmv2:subscription/asmv2:update", nsmgr);
                    if (updateNode != null)
                    {

                        XmlNode ExpirationNode = updateNode.SelectSingleNode("asmv2:expiration", nsmgr);
                        if (ExpirationNode != null)
                            updateNode.RemoveChild(ExpirationNode);


                        //Force app to check for new version everytime at startup
                        LogDebug("Adding beforeApplicationStartup Element...");
                        XmlNode newNode = doc.CreateElement("beforeApplicationStartup", "urn:schemas-microsoft-com:asm.v2");
                        updateNode.AppendChild(newNode);
                    }
                }
                if (MapFileExtensions)
                {
                    LogDebug("Adding mapFileExtensions attribute...");
                    ((XmlElement)deploymentNode).SetAttribute("mapFileExtensions", "true");
                }
            }
            else
            {
                LogError("deployment element not found in manifest");
            }

            doc.Save(deployManifest);
        }

        private static XmlNamespaceManager CreateNamespaceManager(XmlDocument doc)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace(String.Empty, "urn:schemas-microsoft-com:asm.v2");
            nsmgr.AddNamespace("asmv1", "urn:schemas-microsoft-com:asm.v1");
            nsmgr.AddNamespace("asmv2", "urn:schemas-microsoft-com:asm.v2");
            nsmgr.AddNamespace("co.v1", "urn:schemas-microsoft-com:clickonce.v1");
            nsmgr.AddNamespace("co.v2", "urn:schemas-microsoft-com:clickonce.v2");
            return nsmgr;
        }
    }
}
