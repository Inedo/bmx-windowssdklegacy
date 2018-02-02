using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;
using Inedo.Documentation;
using Inedo.Serialization;

namespace Inedo.BuildMasterExtensions.WindowsSdk.DotNet
{
    [DisplayName("Precompile ASP.NET Site")]
    [Description("Precompiles an ASP.NET (2.0 or later) site.")]
    [Tag(Tags.DotNet)]
    [Inedo.Web.CustomEditor(typeof(PrecompileAspNetSiteActionEditor))]
    public sealed class PrecompileAspNetSiteAction : MSBuildActionBase
    {
        [Persistent]
        public string ApplicationVirtualPath { get; set; }

        [Persistent]
        public bool Updatable { get; set; }
        
        [Persistent]
        public bool FixedNames { get; set; }

        public override ExtendedRichDescription GetActionDescription()
        {
            return new ExtendedRichDescription(
                new RichDescription(
                    "Precompile site in ",
                    new DirectoryHilite(this.OverriddenSourceDirectory)
                ),
                new RichDescription(
                    "to ",
                    new DirectoryHilite(this.OverriddenTargetDirectory),
                    " with virtual path ",
                    new Hilite(Util.CoalesceStr(this.ApplicationVirtualPath, "/"))
                )
            );
        }

        protected override void Execute()
        {
            var retVal = string.Empty;

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

        private static string GetShortPath(string path)
        {
            var buffer = new StringBuilder(1000);
            NativeMethods.GetShortPathName(path, buffer, 1000);
            return buffer.ToString();
        }

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
