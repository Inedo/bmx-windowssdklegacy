using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Implements actions for running the .NET NGen utility.
    /// </summary>
    [ActionProperties(
        "NGen",
        "Installs/uninstalls native images from a .NET assembly and its dependencies.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(NGenActionEditor))]
    public sealed class NGenAction : MSBuildActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NGenAction"/> class.
        /// </summary>
        public NGenAction()
        {
        }

        /// <summary>
        /// Gets or sets the path to the target assembly or its strong name if it has been GAC'ed.
        /// </summary>
        [Persistent]
        public string TargetAssembly { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether NGen should run in the background.
        /// </summary>
        [Persistent]
        public bool UseQueue { get; set; }
        /// <summary>
        /// Gets or sets the specific NGen sub-action to perform.
        /// </summary>
        /// <remarks>
        /// If this property is set to Update, TargetAssembly is ignored.
        /// If this property is set to Uninstall, UseQueue is ignored.
        /// </remarks>
        public NGenMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the NGen mode as a string.
        /// </summary>
        /// <remarks>
        /// This property is here to avoid issues with persistence.
        /// </remarks>
        [Persistent]
        internal string RunMode
        {
            get { return this.Mode.ToString(); }
            set { this.Mode = (NGenMode)Enum.Parse(typeof(NGenMode), value); }
        }

        public override ActionDescription GetActionDescription()
        {
            switch (this.Mode)
            {
                case NGenMode.Install:
                    return new ActionDescription(
                        new ShortActionDescription(
                            "Generate Native Image for ",
                            new DirectoryHilite(this.OverriddenSourceDirectory, this.TargetAssembly)
                        )
                    );

                case NGenMode.Uninstall:
                    return new ActionDescription(
                        new ShortActionDescription(
                            "Uninstall Native Images for ",
                            new DirectoryHilite(this.OverriddenSourceDirectory, this.TargetAssembly)
                        )
                    );

                case NGenMode.Update:
                    return new ActionDescription(
                        new ShortActionDescription(
                            "Update the Native Image Cache"
                        )
                    );

                default:
                    return new ActionDescription(new ShortActionDescription());
            }
        }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            ExecuteRemoteCommand("ngen");
        }
        /// <summary>
        /// When implemented in a derived class, processes an arbitrary command
        /// on the appropriate server.
        /// </summary>
        /// <param name="name">Name of command to process.</param>
        /// <param name="args">Optional command arguments.</param>
        /// <returns>Result of the command.</returns>
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            var ngenPath = Path.Combine(GetFrameworkPath(), "ngen.exe");
            if (!File.Exists(ngenPath))
            {
                LogError(string.Format("ngen.exe not found at {0}", ngenPath));
                return string.Empty;
            }

            string ngenArgs;

            switch (this.Mode)
            {
                case NGenMode.Install:
                    if (string.IsNullOrEmpty(this.TargetAssembly))
                        throw new InvalidOperationException("Target assembly not specified.");
                    LogInformation(string.Format("Installing {0} to the native image cache...", this.TargetAssembly));
                    ngenArgs = string.Format("install \"{0}\" {1}", this.TargetAssembly, this.UseQueue ? "/queue" : string.Empty);
                    break;

                case NGenMode.Uninstall:
                    if (string.IsNullOrEmpty(this.TargetAssembly))
                        throw new InvalidOperationException("Target assembly not specified.");
                    LogInformation(string.Format("Uninstalling {0} from the native image cache...", this.TargetAssembly));
                    ngenArgs = string.Format("uninstall \"{0}\"", this.TargetAssembly);
                    break;
                    
                case NGenMode.Update:
                    LogInformation("Updating native images...");
                    ngenArgs = this.UseQueue ? "update /queue" : "update";
                    break;

                default:
                    throw new InvalidOperationException("Invalid NGen action.");
            }

            ExecuteCommandLine(ngenPath, ngenArgs, Path.GetDirectoryName(ngenPath));

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
