using System;
using System.IO;
using System.Linq;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web;
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
        RecipeScopes.System)]
    [CustomEditor(typeof(ExtensionApplicationRecipeEditor))]
    public sealed class ExtensionApplicationRecipe : RecipeBase
    {
        private int applicationId;

        public string ApplicationName { get; set; }
        public string SolutionPath { get; set; }
        public int ScmProviderId { get; set; }
        internal ProjectInfo Project { get; set; }

        public override string RedirectUrl
        {
            get { return string.Format("/applications/{0}/", this.applicationId); }
        }

        public override void Execute()
        {
            var environments = StoredProcs.Environments_GetEnvironments(null).Execute();
            if (environments.Count() < 2)
                throw new InvalidOperationException("At least 2 environments must be created in BuildMaster in order to run this recipe.");

            this.applicationId = Util.Recipes.CreateNewApplication(
                this.ApplicationName, 
                Domains.ReleaseNumberSchemes.MajorMinor, 
                Domains.BuildNumberSchemes.Sequential, 
                true, 
                null
            );            

            int firstEnvironmentId = environments.First().Environment_Id;
            int lastEnvironmentId = environments.Last().Environment_Id;

            int deployableId = Util.Recipes.CreateDeployable(this.applicationId, "Extension");
            int workflowId = Util.Recipes.CreateWorkflow(this.applicationId);
            int buildStepDeploymentPlanId = Util.Recipes.CreateBuildStep(workflowId);
            Util.Recipes.CreateWorkflowStep(workflowId, lastEnvironmentId);
            int finalDeploymentPlanId = Util.Recipes.CreateDeploymentPlanForWorkflowStep(workflowId, 1);

            int actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                buildStepDeploymentPlanId, 
                deployableId: deployableId, 
                name: "Get Extension Source", 
                description: "Actions in this group will first tag/label code files in source control, then retrieve them to the default working directory. The AssemblyInfo.cs file will then be edited to use the current Build/Release number."
            );
            
            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.ApplyLabelAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                    ProviderId = this.ScmProviderId
                }));
            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                    ProviderId = this.ScmProviderId
                }));
            Util.Recipes.AddAction(actionGroupId, 1, new WriteAssemblyInfoVersionsAction
            {
                FileMasks = new[] { @"*\AssemblyInfo.cs" },
                Version = "$ReleaseNumber.$BuildNumber",
                Recursive = true
            });

            actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                buildStepDeploymentPlanId, 
                deployableId: deployableId,
                name: "Build Extension",
                description: "The extension is compiled and then zipped into a .bmx file (the convention for BuildMaster extensions), and then packaged into an artifact."
            );

            Util.Recipes.AddAction(actionGroupId, 1, new BuildMSBuildProjectAction
            {
                ProjectPath = this.Project.FileSystemPath,
                ProjectBuildConfiguration = "Debug",
                IsWebProject = false
            });
            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Files.CreateZipFileAction", new
                {
                    FileName = this.ApplicationName + ".bmx"
                }));
            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                {
                    ArtifactName = "Extension"
                }));

            actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                finalDeploymentPlanId,
                deployableId: deployableId,
                name: "Deploy Extension",
                description: "BuildMaster artifacts are deployed to the local server in the extensions directory."
            );

            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                   "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                   {
                       ArtifactName = "Extension",
                       OverriddenTargetDirectory = GetExtensionsPath()
                   }));

            actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                finalDeploymentPlanId,
                deployableId: deployableId,
                name: "Restart BuildMaster",
                description: "Both the Web Application and Service must be restarted for the extension to be deployed. This is a bit tricky, but this hack will do the trick... most of the time."
            );

            Util.Recipes.AddAction(actionGroupId, 1, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.Files.CreateFileAction", new
                {
                    OverriddenSourceDirectory = GetBinPath(),
                    FileName = "reset",
                    Contents = "kick-service:$Date"
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