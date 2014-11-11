using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Azure
{
    [ActionProperties(
        "Publish Azure Website",
        "Builds and publishes a Windows Azure website.")]
    [Tag(Tags.DotNet)]
    [Tag("azure")]
    [CustomEditor(typeof(PublishAzureWebsiteActionEditor))]
    public sealed class PublishAzureWebsiteAction : MSBuildActionBase
    {
        [Persistent]
        public string ProjectPath { get; set; }
        [Persistent]
        public string ProjectPublishProfileName { get; set; }
        [Persistent]
        public string ProjectPublishProfileXml { get; set; }
        [Persistent]
        public string ProjectBuildConfiguration { get; set; }
        [Persistent]
        public string VisualStudioVersion { get; set; }
        [Persistent]
        public string AdditionalArguments { get; set; }
        [Persistent]
        public string UserName { get; set; }
        [Persistent(Encrypted = true)]
        public string Password { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription("Publish Azure Website"),
                new LongActionDescription("from ", new Hilite(Util.Path2.GetFileName(this.ProjectPath)))
            );
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            this.LogInformation("Publishing to Windows Azure using profile \"{0}\"...", this.ProjectPublishProfileName);

            this.InvokeMSBuild(
                this.BuildArguments(),
                this.Context.SourceDirectory
            );

            return null;
        }

        protected override void Execute()
        {
            this.ExecuteRemoteCommand(null);
        }

        private string BuildArguments()
        {
            var buffer = new StringBuilder();
            buffer.Append(this.ProjectPath);
            buffer.Append(" /p:DeployOnBuild=true /p:AllowUntrustedCertificate=True");
            buffer.AppendFormat(" /p:PublishProfile={0}", this.GetPublishProfilePath());
            buffer.AppendFormat(" /p:Configuration={0}", this.ProjectBuildConfiguration);
            buffer.AppendFormat(" /p:VisualStudioVersion={0}", this.VisualStudioVersion);

            var credentials = this.GetCredentials();
            if (credentials != null)
            {
                buffer.AppendFormat(" /p:UserName={0} /p:Password={1}", credentials.UserName, credentials.Password);
            }
            buffer.Append(" ");
            buffer.Append(this.AdditionalArguments);

            return buffer.ToString();
        }

        private string GetPublishProfilePath()
        {
            if (!string.IsNullOrEmpty(this.ProjectPublishProfileName))
                return this.ProjectPublishProfileName;

            if (string.IsNullOrWhiteSpace(this.ProjectPublishProfileXml))
                throw new InvalidOperationException("Either the publish profile name or custom XML value must be set.");

            string path = Util.Path2.Combine(this.Context.TempDirectory, "buildmaster-azure.pubxml");
            File.WriteAllText(path, this.ProjectPublishProfileXml);

            return path;
        }

        private NetworkCredential GetCredentials()
        {
            if (!string.IsNullOrEmpty(this.UserName))
                return new NetworkCredential(this.UserName, this.Password);

            var configurer = this.GetExtensionConfigurer() as WindowsSdkExtensionConfigurer;
            if (configurer != null)
                return new NetworkCredential(configurer.AzureUserName, configurer.AzurePassword);

            return null;
        }
    }
}
