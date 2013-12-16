using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    /// <summary>
    /// Represents an action that precompiles a ASP.NET 2.0 Web Application.
    /// </summary>
    [ActionProperties(
        "Precompile ASP.NET Site",
        "Precompiles an ASP.NET (2.0 or later) site.")]
    [Tag(Tags.DotNet)]
    [CustomEditor(typeof(PrecompileAspNetSiteActionEditor))]
    public sealed class PrecompileAspNetSiteAction : MSBuildActionBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrecompileAspNetSiteAction"/> class.
        /// </summary>
        public PrecompileAspNetSiteAction()
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(
                "Precompile ASP.NET site in {0} to {1} with virtual path {2}",
                Util.CoalesceStr(this.OverriddenSourceDirectory, "default source directory"),
                Util.CoalesceStr(this.OverriddenTargetDirectory, "default target directory"),
                Util.CoalesceStr(this.ApplicationVirtualPath, "/")
            );
        }

        /// <summary>
        /// Gets or sets the virtual path of the application to be compiled (e.g. "/MyApp"). 
        /// </summary>
        [Persistent]
        public string ApplicationVirtualPath { get; set; }

        /// <summary>
        /// Indicates that the precompiled application is updatable.
        /// </summary>
        [Persistent]
        public bool Updatable { get; set; }
        
        /// <summary>
        /// Indicates that the compiled assemblies will be given fixed names.
        /// </summary>
        [Persistent]
        public bool FixedNames { get; set; }

        /// <summary>
        /// This method is called to execute the Action.
        /// </summary>
        protected override void Execute()
        {
            string retVal = string.Empty;

            // Make sure virtual path starts with a /
            if (string.IsNullOrEmpty(this.ApplicationVirtualPath) || !this.ApplicationVirtualPath.StartsWith("/"))
                this.ApplicationVirtualPath = "/" + this.ApplicationVirtualPath;

            LogInformation("Precompiling site...");
            retVal = ExecuteRemoteCommand("PreCompile");
            if (retVal != "0")
            {
                throw new Exception(string.Format(
                    "Step Failed (aspnet_compiler returned code {0})",
                    retVal));
            }
        }

        /// <summary>
        /// When implemented in a derived class, processes an arbitrary command
        /// on the appropriate server.
        /// </summary>
        /// <param name="name">Name of command to process.</param>
        /// <param name="args">Optional command arguments.</param>
        /// <returns>
        /// Result of the command.
        /// </returns>
        protected override string ProcessRemoteCommand(string name, string[] args)
        {
            int retVal = 0;

            switch (name)
            {
                case "PreCompile":
                    var cmdargs = new StringBuilder();
                    cmdargs.AppendFormat(" -v \"{0}\"", this.ApplicationVirtualPath);
                    cmdargs.AppendFormat(" -p {0}", GetShortPath(this.Context.SourceDirectory));
                    if (this.Updatable) cmdargs.Append(" -u");
                    if (this.FixedNames) cmdargs.Append(" -fixednames");
                    cmdargs.AppendFormat(" {0}", GetShortPath(this.Context.TargetDirectory));

                    retVal = ExecuteCommandLine(
                        GetAspNetCompilerPath(),
                        cmdargs.ToString(),
                        this.Context.SourceDirectory);
                   break;

                default:
                    throw new ArgumentOutOfRangeException("name");
            }

            return retVal.ToString();
        }

        /// <summary>
        /// Returns a short path of a given path.
        /// </summary>
        /// <param name="path">Path to convert to a short path.</param>
        /// <returns>Short path of the specified path.</returns>
        private static string GetShortPath(string path)
        {
            var buffer = new StringBuilder(1000);
            NativeMethods.GetShortPathName(path, buffer, 1000);
            return buffer.ToString();
        }

        /// <summary>
        /// Gets the ASP.NET compiler path.
        /// </summary>
        /// <returns>The ASP.NET compiler path.</returns>
        private string GetAspNetCompilerPath()
        {
            var frameworkPath = GetFrameworkPath();
            return Path.Combine(frameworkPath, "aspnet_compiler.exe");
        }

        private static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            public static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);
        }
    }
}
