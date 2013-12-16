using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI.WebControls;
using ICSharpCode.SharpZipLib.Zip;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web;
using Inedo.Web.Controls;
using Inedo.Web.Handlers;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    internal sealed class ExtensionApplicationWizardSteps : RecipeWizardSteps
    {
        public readonly RecipeWizardStep SelectOrganizationName = new RecipeWizardStep("Extension Name");
        public readonly DownloadInstructionsStep DownloadInstructions = new DownloadInstructionsStep("Download Instructions");
        public readonly RecipeWizardStep SelectProviderAndSolution = new RecipeWizardStep("Select Provider/Solution");
        public readonly RecipeWizardStep OneProjectVerification = new RecipeWizardStep("Extension Project");
        public readonly RecipeWizardStep Confirmation = new RecipeWizardStep("Summary");

        public override RecipeWizardStep[] WizardStepOrder
        {
            get
            {
                return new[] { this.SelectOrganizationName, this.DownloadInstructions, this.SelectProviderAndSolution, this.OneProjectVerification, this.Confirmation };
            }
        }

        internal sealed class DownloadInstructionsStep : RecipeWizardStep
        {
            public DownloadInstructionsStep(string title) : base(title)
            { 
            }

            public string OrganizationName { get; set; }

            protected override void CreateChildControls()
            {
                this.Controls.AddLiteral("<style type=\"text/css\"> p { margin: 20px 0; } </style>");
                this.Controls.AddHtml("p", "In order to build this project, you must have a source control provider setup within BuildMaster. On the next step, you will need to point to the location in source control where your extension project is stored. If you haven't added this project to source control, you can download the starter project by clicking the button below.");

                var lnk = new HyperLink()
                    {
                        CssClass = "buttonMinor",
                        Text = "Download Starter Project"
                    };
                lnk.PreRender += (s, e) => lnk.NavigateUrl = GetDownloadResourceUrl(KramericaDownloadHandler) + "?name=" + HttpUtility.UrlEncode(this.OrganizationName);
                this.Controls.Add(lnk);

                this.Controls.AddHtml("p", "Once you have downloaded the project and added it to a source control provider, and you'll be able to select its path in the next step. The project is for Visual Studio 2010 and written in C#.");
                this.Controls.AddLiteral("<p>For more information on creating an extension, please see our <a href=\"http://links.inedo.com/buildmaster-sdk-example\" target=\"_blank\">BuildMaster SDK example tutorial</a>.</p>");
            }

            private static string GetDownloadResourceUrl(ProcessRequestDelegate del)
            {
                return string.Format("/0x44/{0}/{1}/{2}",
                    PathEncoding.Encode(del.Method.DeclaringType.Assembly.GetName().Name),
                    PathEncoding.Encode(del.Method.DeclaringType.FullName),
                    PathEncoding.Encode(del.Method.Name)
                );
            }

            private static void KramericaDownloadHandler(HttpContext context)
            {
                var replacementName = Regex.Replace(context.Request.QueryString["name"], @"[\W]", "");

                context.Response.Clear();
                context.Response.AddHeader("Content-Disposition", "attachment; filename=" + replacementName + "Extension.zip");
                context.Response.ContentType = MimeMapping.GetMimeMapping("KramericaExtension.zip");
                using (var input = new MemoryStream(Properties.Resources.KramericaExtension, false))
                using (var output = new MemoryStream())
                {
                    MungeKramericaZip(input, output, replacementName);
                    output.WriteTo(context.Response.OutputStream);
                }
            }
            private static void MungeKramericaZip(Stream inputStream, Stream outputStream, string replacementName)
            {
                using (var izip = new ZipFile(inputStream))
                using (var ozip = new ZipOutputStream(outputStream))
                {
                    izip.IsStreamOwner = false;
                    ozip.IsStreamOwner = false;

                    foreach (ZipEntry ze in izip)
                    {
                        byte[] zeBytes;
                        using (var zeStream = izip.GetInputStream(ze))
                        {
                            zeBytes = new byte[ze.Size];
                            zeStream.Read(zeBytes, 0, zeBytes.Length);
                        }

                        if (ze.IsFile && (ze.Name.EndsWith(".cs") || ze.Name.EndsWith(".csproj") || ze.Name.EndsWith(".sln")))
                        {
                            zeBytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(zeBytes).Replace("Kramerica", replacementName));
                        }

                        var newZipEntry = new ZipEntry(ze.Name.Replace("Kramerica", replacementName));
                        newZipEntry.DateTime = DateTime.Now;
                        newZipEntry.Size = zeBytes.Length;

                        ozip.PutNextEntry(newZipEntry);
                        ozip.Write(zeBytes, 0, zeBytes.Length);
                        ozip.CloseEntry();
                    }
                    ozip.Close();
                }
            }

            /// <summary>
            /// Copied from WUtil in BuildMaster solution to use delegate handling
            /// </summary>
            private static class PathEncoding
            {
                private static Regex escapedRegex = new Regex(" 0x(?<hex>[0-9a-f][0-9a-f]) ", RegexOptions.IgnoreCase | RegexOptions.Compiled);

                private static char[] INVALID_CHARS =
                    new char[] {'"','<','>','%','\\','^','[',']','`',
                        '$',';','/','?',':','@','=','&',
                        '#','*','_' /*  Removed . and + (seems to work) */};
                public static string Decode(string val)
                {
                    return escapedRegex.Replace(val.Replace('_', ' '), DecodeMatch);
                }
                public static string Encode(string val)
                {
                    //check if name contains hex escapes
                    if (escapedRegex.IsMatch(val))
                    {
                        //escape out the "x" (ASC 78) using spaces, which get replaced
                        //with "_" anyway
                        val = escapedRegex.Replace(val, " 0 0x78 ${hex} ");
                    }
                    StringBuilder ret = new StringBuilder();
                    foreach (char chr in val)
                    {
                        if ((new string(INVALID_CHARS)).IndexOf(chr) != -1)
                            ret.Append(EncodeChar(chr));
                        else if (chr == ' ') ret.Append('_');
                        else ret.Append(chr);
                    }

                    return ret.ToString();
                }
                private static string EncodeChar(char chr)
                {
                    return string.Format("_0x{0}_", Convert.ToInt32(chr).ToString("x"));

                }
                private static string DecodeMatch(Match match)
                {
                    int val = Convert.ToInt32(match.Groups["hex"].Value, 16);
                    return Convert.ToChar(val).ToString();
                }
            }
        }
    }
}
