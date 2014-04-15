using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
using Inedo.BuildMaster.Extensibility.Actions;
using Inedo.BuildMaster.Extensibility.Providers.SourceControl;
using Inedo.BuildMaster.Extensibility.Recipes;
using Inedo.BuildMaster.Web;
using Inedo.BuildMasterExtensions.WindowsSdk.DotNet;
using Inedo.BuildMasterExtensions.WindowsSdk.MSBuild;

namespace Inedo.BuildMasterExtensions.WindowsSdk.Recipes
{
    [RecipeProperties(
        ".NET Application",
        "A wizard that reads an existing solution or project file in source control to create skeleton deployment plans for building and deploying a .NET application.",
        RecipeScopes.NewApplication)]
    [CustomEditor(typeof(CreateNetApplicationRecipeEditor))]
    public sealed class CreateNetApplicationRecipe : RecipeBase, IApplicationCreatingRecipe, IWorkflowCreatingRecipe
    {
        public string ApplicationGroup { get; set; }
        public string ApplicationName { get; set; }
        public int ApplicationId { get; set; }
        public string WorkflowName { get; set; }
        public int[] WorkflowSteps { get; set; }
        public int WorkflowId { get; set; }

        internal int ScmProviderId { get; set; }
        internal string SolutionPath { get; set; }
        internal ProjectInfo[] Projects { get; set; }

        public override void Execute()
        {
            try
            {
                this.ExecuteRecipe();
            }
            catch
            {
                try { StoredProcs.Applications_PurgeApplicationData(this.ApplicationId).ExecuteNonQuery(); }
                catch { }

                throw;
            }
        }

        private void ExecuteRecipe()
        {
            var deployableIds = new int[this.Projects.Length];
            var configIds = new int[this.Projects.Length][];

            var environmentNames = StoredProcs
                .Environments_GetEnvironments(null)
                .Execute()
                .ToDictionary(e => e.Environment_Id, e => e.Environment_Name);

            for (int i = 0; i < this.Projects.Length; i++)
                deployableIds[i] = Util.Recipes.CreateDeployable(this.ApplicationId, this.Projects[i].Name);

            Util.Recipes.CreateSetupRelease(
                this.ApplicationId,
                Domains.ReleaseNumberSchemes.MajorMinor,
                this.WorkflowId,
                deployableIds
            );

            for (int i = 0; i < this.Projects.Length; i++)
            {
                configIds[i] = new int[this.Projects[i].ConfigFiles.Count];
                if (configIds[i].Length > 0)
                {
                    using (var proxy = Util.Proxy.CreateProviderProxy(this.ScmProviderId))
                    {
                        var scm = proxy.TryGetService<SourceControlProviderBase>();

                        for (int j = 0; j < this.Projects[i].ConfigFiles.Count; j++)
                        {
                            var createConfigFile = StoredProcs.ConfigurationFiles_CreateConfigurationFile(
                                null,
                                deployableIds[i],
                                this.Projects[i].ConfigFiles[j].Replace((char)scm.DirectorySeparator, '\\')
                            );
                            createConfigFile.ExecuteNonQuery();
                            configIds[i][j] = (int)createConfigFile.ConfigurationFile_Id;

                            foreach (int envId in this.WorkflowSteps)
                            {
                                StoredProcs.ConfigurationFiles_CreateConfigurationFileInstance(
                                    configIds[i][j],
                                    environmentNames[envId],
                                    envId,
                                    Domains.YN.No,
                                    null
                                ).ExecuteNonQuery();
                            }

                            var configFileBytes = (byte[])scm.GetFileContents(this.SolutionPath + scm.DirectorySeparator + this.Projects[i].ScmDirectoryName + scm.DirectorySeparator + this.Projects[i].ConfigFiles[j]);
                            AddConfigurationFile(configIds[i][j], "0.0", this.WorkflowSteps.Select(s => environmentNames[s]), configFileBytes);
                        }
                    }
                }
            }

            int getSourcePlanId = this.CreatePlan(null, this.WorkflowSteps[0], "Get Source", "Applies a label to the files in Source Control and gets a copy of the label.");
            AddAction(getSourcePlanId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.ApplyLabelAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                    ProviderId = this.ScmProviderId
                }));
            AddAction(getSourcePlanId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                    ProviderId = this.ScmProviderId,
                    OverriddenTargetDirectory = @"~\Src"
                }));
            AddAction(getSourcePlanId, new WriteAssemblyInfoVersionsAction
            {
                OverriddenSourceDirectory = @"~\Src",
                FileMasks = new[] { @"*\AssemblyInfo.cs", @"*\AssemblyInfo.vb" },
                Version = "$ReleaseNumber.$BuildNumber",
                Recursive = true
            });

            for (int i = 0; i < deployableIds.Length; i++)
            {
                int buildPlanId = this.CreatePlan(deployableIds[i], this.WorkflowSteps[0], "Build " + this.Projects[i].Name, string.Format("Builds {0} and creates an artifact from the build output.", this.Projects[i].Name));
                AddAction(buildPlanId, new BuildMSBuildProjectAction
                {
                    OverriddenSourceDirectory = @"~\Src",
                    ProjectPath = this.Projects[i].FileSystemPath,
                    ProjectBuildConfiguration = "Debug",
                    IsWebProject = this.Projects[i].IsWebApplication
                });
                AddAction(buildPlanId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                    {
                        ArtifactName = this.Projects[i].Name
                    }));
            }

            for (int i = 0; i < deployableIds.Length; i++)
            {
                int deployPlanId = this.CreatePlan(deployableIds[i], this.WorkflowSteps[0], "Deploy " + this.Projects[i].Name, string.Format("Deploys {0}.", this.Projects[i].Name));
                AddAction(deployPlanId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                    {
                        ArtifactName = this.Projects[i].Name,
                        OverriddenTargetDirectory = this.Projects[i].DeploymentTarget
                    }));

                foreach (int configFileId in configIds[i])
                {
                    AddAction(deployPlanId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Configuration.DeployConfigurationFileAction", new
                    {
                        ConfigurationFileId = configFileId,
                        InstanceName = environmentNames[this.WorkflowSteps[0]],
                        OverriddenSourceDirectory = this.Projects[i].DeploymentTarget
                    }));
                }
            }
        }
        private int CreatePlan(int? deployableId, int environmentId, string planName, string planDesc)
        {
            var proc = StoredProcs.Plans_CreatePlanActionGroup(
                Deployable_Id: deployableId,
                Environment_Id: environmentId,
                Application_Id: this.ApplicationId,
                Active_Indicator: Domains.YN.Yes,
                ActionGroup_Name: planName,
                ActionGroup_Description: planDesc
            );
            proc.ExecuteNonQuery();
            return proc.ActionGroup_Id.Value;
        }
        private static int AddAction(int planId, ActionBase action)
        {
            var proc = StoredProcs.Plans_CreateOrUpdateAction(
                Plan_Id: planId,
                Server_Id: action is AgentBasedActionBase ? (int?)1 : null,
                Action_Description: action.ToString(),
                ResumeNextOnFailure_Indicator: Domains.YN.No,
                Action_Configuration: Util.Persistence.SerializeToPersistedObjectXml(action),
                ActionType_Name: Util.Reflection.GetCustomAttribute<ActionPropertiesAttribute>(action.GetType()).Name,
                Active_Indicator: Domains.YN.Yes,
                Retry_Count: 0,
                LogFailureAsWarning_Indicator: Domains.YN.No
            );
            proc.ExecuteNonQuery();
            return proc.Action_Sequence.Value;
        }
        private void AddConfigurationFile(int configFileId, string releaseNumber, IEnumerable<string> instanceNames, byte[] fileBytes)
        {
            var configBuffer = new StringBuilder();
            using (var configXmlWriter = XmlWriter.Create(configBuffer, new XmlWriterSettings { OmitXmlDeclaration = true, NewLineHandling = NewLineHandling.None, CloseOutput = true }))
            {
                configXmlWriter.WriteStartElement("ConfigFiles");

                var base64 = Convert.ToBase64String(fileBytes);

                foreach (var instanceName in instanceNames)
                {
                    configXmlWriter.WriteStartElement("Version");
                    configXmlWriter.WriteAttributeString("Instance_Name", instanceName);
                    configXmlWriter.WriteAttributeString("Release_Number", releaseNumber);
                    configXmlWriter.WriteAttributeString("VersionNotes_Text", null);
                    configXmlWriter.WriteAttributeString("File_Bytes", base64);
                    configXmlWriter.WriteEndElement();
                }

                configXmlWriter.WriteEndElement();
            }

            StoredProcs.ConfigurationFiles_CreateConfigurationFileVersions(
                configFileId,
                configBuffer.ToString()
            ).ExecuteNonQuery();
        }
    }
}
