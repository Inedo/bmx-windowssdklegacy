using System.IO;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Agents;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.WindowsSdk
{
    [ActionProperties(
        "Transform Configuration File",
        "Performs an XDT transform on a configuration file.")]
    [Tag(Tags.ConfigurationFiles)]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(XdtTransformActionEditor))]
    public sealed class XdtTransformAction : AgentBasedActionBase
    {
        [Persistent]
        public string SourceFile { get; set; }

        [Persistent]
        public string TransformFile { get; set; }

        [Persistent]
        public string DestinationFile { get; set; }

        [Persistent]
        public bool PreserveWhitespace { get; set; }

        [Persistent]
        public bool Verbose { get; set; }

        public override ActionDescription GetActionDescription()
        {
            return new ActionDescription(
                new ShortActionDescription(
                    "XDT Transform ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.SourceFile)
                ),
                new LongActionDescription(
                    "to ",
                    new DirectoryHilite(this.OverriddenTargetDirectory, this.DestinationFile),
                    " using ",
                    new DirectoryHilite(this.OverriddenSourceDirectory, this.TransformFile)
                )
            );
        }

        protected override void Execute()
        {
            var fileOps = this.Context.Agent.GetService<IFileOperationsExecuter>();

            var transformExePath = Path.Combine(
                    fileOps.GetBaseWorkingDirectory(),
                    @"ExtTemp\WindowsSdk\Resources\ctt.exe"
            );

            if (!fileOps.FileExists(transformExePath))
                throw new FileNotFoundException("ctt.exe could not be found on the agent.", transformExePath);

            string arguments = BuildArguments();

            this.LogInformation("Performing XDT transform...");

            this.ExecuteCommandLine(transformExePath, arguments);
        }

        private string BuildArguments()
        {
            var buffer = new StringBuilder();
            buffer.AppendFormat("source:\"{0}\"", Path.Combine(this.Context.SourceDirectory, this.SourceFile));
            buffer.AppendFormat(" transform:\"{0}\"", Path.Combine(this.Context.SourceDirectory, this.TransformFile));
            buffer.AppendFormat(" destination:\"{0}\"", Path.Combine(this.Context.TargetDirectory, this.DestinationFile));
            buffer.Append(" indent");
            if (this.PreserveWhitespace)
                buffer.Append(" preservewhitespace");
            if (this.Verbose)
                buffer.Append(" verbose");

            return buffer.ToString();
        }
    }
}
