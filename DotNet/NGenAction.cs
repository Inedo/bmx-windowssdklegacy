using System;
using System.ComponentModel;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Documentation;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [DisplayName("NGen")]
    [Description("Installs/uninstalls native images from a .NET assembly and its dependencies.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(NGenActionEditor))]
    public sealed class NGenAction : MSBuildActionBase
    {
        [Persistent]
        public string TargetAssembly { get; set; }
        [Persistent]
        public bool UseQueue { get; set; }
        [Persistent]
        public NGenMode RunMode { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            switch (this.RunMode)
            {
                case NGenMode.Install:
                    return new ExtendedRichDescription(
                        new RichDescription(
                            "Generate Native Image for ",
                            new DirectoryHilite(this.OverriddenSourceDirectory, this.TargetAssembly)
                        )
                    );

                case NGenMode.Uninstall:
                    return new ExtendedRichDescription(
                        new RichDescription(
                            "Uninstall Native Images for ",
                            new DirectoryHilite(this.OverriddenSourceDirectory, this.TargetAssembly)
                        )
                    );

                case NGenMode.Update:
                    return new ExtendedRichDescription(
                        new RichDescription(
                            "Update the Native Image Cache"
                        )
                    );

                default:
                    return new ExtendedRichDescription(new RichDescription());
            }
        }

        protected override void Execute()
        {
            this.ExecuteRemoteCommand("ngen");
        }
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var ngenPath = Path.Combine(this.GetFrameworkPath(), "ngen.exe");
            if (!File.Exists(ngenPath))
            {
                this.LogError("ngen.exe not found at {0}", ngenPath);
                return string.Empty;
            }

            string ngenArgs;

            switch (this.RunMode)
            {
                case NGenMode.Install:
                    if (string.IsNullOrEmpty(this.TargetAssembly))
                        throw new InvalidOperationException("Target assembly not specified.");

                    this.LogInformation(string.Format("Installing {0} to the native image cache...", this.TargetAssembly));
                    ngenArgs = string.Format("install \"{0}\" {1}", this.TargetAssembly, this.UseQueue ? "/queue" : string.Empty);
                    break;

                case NGenMode.Uninstall:
                    if (string.IsNullOrEmpty(this.TargetAssembly))
                        throw new InvalidOperationException("Target assembly not specified.");
                    
                    this.LogInformation(string.Format("Uninstalling {0} from the native image cache...", this.TargetAssembly));
                    ngenArgs = string.Format("uninstall \"{0}\"", this.TargetAssembly);
                    break;
                    
                case NGenMode.Update:
                    this.LogInformation("Updating native images...");
                    ngenArgs = this.UseQueue ? "update /queue" : "update";
                    break;

                default:
                    throw new InvalidOperationException("Invalid NGen action.");
            }

            this.ExecuteCommandLine(ngenPath, ngenArgs, Path.GetDirectoryName(ngenPath));

            return string.Empty;
        }
    }

    /// <summary>
    /// Specifies the NGen action to perform.
    /// </summary>
    public enum NGenMode
    {
        /// <summary>
        /// Installs native images to the native image cache.
        /// </summary>
        Install,
        /// <summary>
        /// Uninstalls native images from the native image cache.
        /// </summary>
        Uninstall,
        /// <summary>
        /// Updates invalid native images.
        /// </summary>
        Update
    }
}
