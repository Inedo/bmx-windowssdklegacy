using System.ComponentModel;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.ActionImporters;
using Inedo.Documentation;
using Inedo.IO;
using Inedo.Serialization;
using Inedo.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    [DisplayName("Sign Executable")]
    [Description("Signs an executable file with a certificate.")]
    [Tag(Tags.Windows)]
    [Inedo.Web.CustomEditor(typeof(SignExeActionEditor))]
    [ConvertibleToOperation(typeof(SignExeImporter))]
    public sealed class SignExeAction : AgentBasedActionBase
    {
        [Persistent]
        public string SubjectName { get; set; }
        [Persistent]
        public string TimestampServer { get; set; }
        [Persistent]
        public string ContentDescription { get; set; }
        [Persistent]
        public string ContentUrl { get; set; }
        [Persistent]
        public string SignExePath { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Sign ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.SignExePath)
                ),
                new RichDescription(
                    "using the ",
                    new Hilite(this.SubjectName),
                    " certificate"
                )
            );
        }

        protected override void Execute()
        {
            var args = new StringBuilder("sign /sm");
            args.AppendFormat(" /n \"{0}\"", this.SubjectName);

            if (!string.IsNullOrEmpty(this.TimestampServer))
                args.AppendFormat(" /t \"{0}\"", this.TimestampServer);

            if (!string.IsNullOrEmpty(this.ContentDescription))
                args.AppendFormat(" /d \"{0}\"", this.ContentDescription);

            if (!string.IsNullOrEmpty(this.ContentUrl))
                args.AppendFormat(" /du \"{0}\"", this.ContentUrl);

            args.AppendFormat(" \"{0}\"", this.SignExePath);

            var configurer = (WindowsSdkExtensionConfigurer)this.GetExtensionConfigurer();
            var signExePath = PathEx.Combine(configurer.WindowsSdkPath, "bin\\signtool.exe");

            int result = this.ExecuteCommandLine(signExePath, args.ToString());
            if (result != 0)
                this.LogError("Signtool.exe returned error code {0}", result);
        }
    }
}
