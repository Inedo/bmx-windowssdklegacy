using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;
using Inedo.Documentation;
using Inedo.IO;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Azure
{
    [DisplayName("Publish Azure Website")]
    [Description("Builds and publishes a Windows Azure website.")]
    [Tag(Tags.DotNet)]
    [Tag("azure")]
    [CustomEditor(typeof(PublishAzureWebsiteActionEditor))]
    [PersistFrom("Inedo.BuildMasterExtensions.WindowsSdk.Azure.PublishAzureWebsiteAction,WindowsSdk")]
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

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription("Publish Azure Website"),
                new RichDescription("from ", new Hilite(PathEx.GetFileName(this.ProjectPath)))
            );
        }

        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            this.LogInformation($"Publishing to Windows Azure using profile \"{this.ProjectPublishProfileName}\"...");

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

            string path = PathEx.Combine(this.Context.TempDirectory, "buildmaster-azure.pubxml");
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
