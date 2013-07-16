using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    /// <summary>
    /// Action that signs an executable file with a certificate.
    /// </summary>
    [ActionProperties(
        "Sign Executable",
        "Signs an executable file with a certificate.",
        "Windows")]
    public sealed class SignExeAction : AgentBasedActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignExeAction"/> class.
        /// </summary>
        public SignExeAction()
        {
        }

        /// <summary>
        /// Gets the subject name (or a substring of the subject name) of the signing certificate.
        /// </summary>
        [Persistent]
        public string SubjectName { get; set; }
        /// <summary>
        /// Gets the URL of the timestamp server used when signing the executable. If not specified, no timestamp is used.
        /// </summary>
        [Persistent]
        public string TimestampServer { get; set; }
        /// <summary>
        /// Gets the description of the signed content.
        /// </summary>
        [Persistent]
        public string ContentDescription { get; set; }
        /// <summary>
        /// Gets a URL with more information about the signed content.
        /// </summary>
        [Persistent]
        public string ContentUrl { get; set; }
        /// <summary>
        /// Gets the path of the executable file to sign (relative to the source directory).
        /// </summary>
        [Persistent]
        public string SignExePath { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            string path;
            if (!string.IsNullOrEmpty(this.OverriddenSourceDirectory))
                path = Util.Path2.Combine(this.OverriddenSourceDirectory, this.SignExePath);
            else
                path = this.SignExePath;

            return string.Format("Sign {0} using the the {1} certificate", path, this.SubjectName);
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
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
            var signExePath = Util.Path2.Combine(configurer.WindowsSdkPath, "bin\\signtool.exe");

            int result = this.ExecuteCommandLine(signExePath, args.ToString());
            if (result != 0)
                this.LogError("Signtool.exe returned error code {0}", result);
        }
    }
}
