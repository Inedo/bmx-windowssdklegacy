using System;
using System.IO;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web;
using System.Linq;
using Inedo.BuildMasterExtensions.WindowsSdk.DotNet;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    /// <summary>
    /// Represents a recipe that creates a simple web application.
    /// </summary>
    [RecipeProperties(
        "Custom Extension Application",
        "An example application that builds an example BuildMaster extension and deploys it to this installation once it has been downloaded and put into your source control provider of choice.",
        RecipeScopes.NewApplication)]
    [CustomEditor(typeof(ExtensionApplicationRecipeEditor))]
    public sealed class ExtensionApplicationRecipe : RecipeBase
    {
        private int applicationId;

        public string OrganizationName { get; set; }
        public string SolutionPath { get; set; }
        public int ScmProviderId { get; set; }
        internal ProjectInfo Project { get; set; }

        public override string RedirectUrl { get { return string.Format("/Applications/{0}/Overview.aspx", this.applicationId); } }

        public override void Execute()
        {
            string extensionName = this.OrganizationName + "Extension";

            this.applicationId = Util.Recipes.CreateNewApplication(
                extensionName, 
                Domains.ReleaseNumberSchemes.MajorMinor, 
                Domains.BuildNumberSchemes.Sequential, 
                true, 
                null
            );

            var environments = StoredProcs.Environments_GetEnvironments(null).Execute();
            int firstEnvironmentId = environments.First().Environment_Id;
            int lastEnvironmentId = environments.Last().Environment_Id;

            int deployableId = Util.Recipes.CreateDeployable(this.applicationId, "Extension");
            int workflowId = Util.Recipes.CreateWorkflow(this.applicationId);
            Util.Recipes.CreateWorkflowStep(workflowId, firstEnvironmentId);
            Util.Recipes.CreateWorkflowStep(workflowId, lastEnvironmentId);

            int planId = Util.Recipes.CreatePlan(
                this.applicationId,
                deployableId,
                firstEnvironmentId,
                "Get Extension Source",
                "Actions in this group will first tag/label code files in source control, then retrieve them to the default working directory. The AssemblyInfo.cs file will then be edited to use the current Build/Release number."
            );
            Util.Recipes.AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.ApplyLabelAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "%RELNO%.%BLDNO%",
                    ProviderId = this.ScmProviderId
                }));
            Util.Recipes.AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "%RELNO%.%BLDNO%",
                    ProviderId = this.ScmProviderId
                }));
            Util.Recipes.AddAction(planId, new WriteAssemblyInfoVersionsAction
            {
                FileMasks = new[] { @"*\AssemblyInfo.cs" },
                Version = "%RELNO%.%BLDNO%",
                Recursive = true
            });

            planId = Util.Recipes.CreatePlan(
                this.applicationId,
                deployableId,
                firstEnvironmentId,
                "Build Extension",
                "The extension is compiled and then zipped into a .bmx file (the convention for BuildMaster extensions), and then packaged into an artifact."
            );
            Util.Recipes.AddAction(planId, new BuildMSBuildProjectAction
            {
                ProjectPath = this.Project.FileSystemPath,
                ProjectBuildConfiguration = "Debug",
                IsWebProject = false
            });
            Util.Recipes.AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Files.CreateZipFileAction", new
                {
                    FileName = extensionName + ".bmx"
                }));
            Util.Recipes.AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                {
                    ArtifactName = "Extension"
                }));

            planId = Util.Recipes.CreatePlan(
                this.applicationId,
                deployableId,
                lastEnvironmentId,
                "Deploy Extension",
                "BuildMaster artifacts are deployed to the local server in the extensions directory."
            );
            Util.Recipes.AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                   "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                   {
                       ArtifactName = "Extension",
                       OverriddenTargetDirectory = GetExtensionsPath()
                   }));

            planId = Util.Recipes.CreatePlan(
                this.applicationId,
                deployableId,
                lastEnvironmentId,
                "Restart BuildMaster",
                "Both the Web Application and Service must be restarted for the extension to be deployed. This is a bit tricky, but this hack will do the trick... most of the time."
            );
            Util.Recipes.AddAction(planId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Files.CreateFileAction", new
                {
                    OverriddenSourceDirectory = GetBinPath(),
                    FileName = "reset",
                    Contents = "kick-service:%DATE:G%"
                }));

            Util.Recipes.CreateRelease(
                "0.0", 
                this.applicationId, 
                deployableId, 
                workflowId, 
                Domains.DeployableInclusionTypes.Included
            );
        }

        private static string GetBinPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
        }

        private static string GetExtensionsPath()
        {
            var proc = StoredProcs.Configuration_GetValue("CoreEx", "ExtensionsPath", null);
            proc.ExecuteNonQuery();
            return proc.Value_Text;
        }
    }
}