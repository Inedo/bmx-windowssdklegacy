using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Data;
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
                                ConfigurationFile_Id: null,
                                Deployable_Id: deployableIds[i],
                                FilePath_Text: this.Projects[i].ConfigFiles[j].Replace((char)scm.DirectorySeparator, '\\'),
                                ConfigurationFile_Name: null,
                                Description_Text: null
                            );
                            configIds[i][j] = (int)createConfigFile.Execute();

                            foreach (int envId in this.WorkflowSteps)
                            {
                                StoredProcs.ConfigurationFiles_CreateConfigurationFileInstance(
                                    ConfigurationFile_Id: configIds[i][j],
                                    Instance_Name: environmentNames[envId],
                                    Environment_Id: envId,
                                    Template_Indicator: Domains.YN.No,
                                    Template_Instance_Name: null,
                                    TransformType_Code: null
                                ).Execute();
                            }

                            var configFileBytes = (byte[])scm.GetFileContents(this.SolutionPath + scm.DirectorySeparator + this.Projects[i].ScmDirectoryName + scm.DirectorySeparator + this.Projects[i].ConfigFiles[j]);
                            AddConfigurationFile(configIds[i][j], "0.0", this.WorkflowSteps.Select(s => environmentNames[s]), configFileBytes);
                        }
                    }
                }
            }

            int firstDeploymentPlanId = Util.Recipes.CreateDeploymentPlanForWorkflowStep(this.WorkflowId, 1); 

            for (int i = 2; i <= this.WorkflowSteps.Length; i++)
                Util.Recipes.CreateDeploymentPlanForWorkflowStep(this.WorkflowId, i);

            int actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                firstDeploymentPlanId, 
                deployableId: null,
                name: "Get Source", 
                description: ""
            );

            //int getSourcePlanId = this.CreatePlan(null, this.WorkflowSteps[0], "", "");
            Util.Recipes.AddAction(actionGroupId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.ApplyLabelAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                    ProviderId = this.ScmProviderId
                }));
            Util.Recipes.AddAction(actionGroupId, Util.Recipes.Munging.MungeCoreExAction(
                "Inedo.BuildMaster.Extensibility.Actions.SourceControl.GetLabeledAction", new
                {
                    SourcePath = this.SolutionPath,
                    UserDefinedLabel = "$ReleaseNumber.$BuildNumber",
                    ProviderId = this.ScmProviderId,
                    OverriddenTargetDirectory = @"~\Src"
                }));
            Util.Recipes.AddAction(actionGroupId, new WriteAssemblyInfoVersionsAction
            {
                OverriddenSourceDirectory = @"~\Src",
                FileMasks = new[] { @"*\AssemblyInfo.cs", @"*\AssemblyInfo.vb" },
                Version = "$ReleaseNumber.$BuildNumber",
                Recursive = true
            });

            for (int i = 0; i < deployableIds.Length; i++)
            {
                actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                    firstDeploymentPlanId,
                    deployableId: deployableIds[i],
                    name: "Build " + this.Projects[i].Name,
                    description: string.Format("Builds {0} and creates an artifact from the build output.", this.Projects[i].Name)
                );

                Util.Recipes.AddAction(actionGroupId, new BuildMSBuildProjectAction
                {
                    OverriddenSourceDirectory = @"~\Src",
                    ProjectPath = this.Projects[i].FileSystemPath,
                    ProjectBuildConfiguration = "Debug",
                    IsWebProject = this.Projects[i].IsWebApplication
                });
                Util.Recipes.AddAction(actionGroupId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.CreateArtifactAction", new
                    {
                        ArtifactName = this.Projects[i].Name
                    }));
            }

            for (int i = 0; i < deployableIds.Length; i++)
            {
                actionGroupId = Util.Recipes.CreateDeploymentPlanActionGroup(
                    firstDeploymentPlanId,
                    deployableId: deployableIds[i],
                    name: "Deploy " + this.Projects[i].Name,
                    description: string.Format("Deploys {0}.", this.Projects[i].Name)
                );

                Util.Recipes.AddAction(actionGroupId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Artifacts.DeployArtifactAction", new
                    {
                        ArtifactName = this.Projects[i].Name,
                        OverriddenTargetDirectory = this.Projects[i].DeploymentTarget
                    }));

                foreach (int configFileId in configIds[i])
                {
                    Util.Recipes.AddAction(actionGroupId, Util.Recipes.Munging.MungeCoreExAction(
                    "Inedo.BuildMaster.Extensibility.Actions.Configuration.DeployConfigurationFileAction", new
                    {
                        ConfigurationFileId = configFileId,
                        InstanceName = environmentNames[this.WorkflowSteps[0]],
                        OverriddenSourceDirectory = this.Projects[i].DeploymentTarget
                    }));
                }
            }
        }
        private void AddConfigurationFile(int configFileId, string releaseNumber, IEnumerable<string> instanceNames, byte[] fileBytes)
        {
            var configBuffer = new StringBuilder();
            using (var configXmlWriter = XmlWriter.Create(configBuffer, new XmlWriterSettings { OmitXmlDeclaration = true, Indent = false, CloseOutput = true }))
            {
                configXmlWriter.WriteStartElement("ConfigFiles");

                var base64 = Convert.ToBase64String(fileBytes);

                foreach (var instanceName in instanceNames)
                {
                    configXmlWriter.WriteStartElement("Version");
                    configXmlWriter.WriteAttributeString("Instance_Name", instanceName);
                    configXmlWriter.WriteAttributeString("VersionNotes_Text", null);
                    configXmlWriter.WriteAttributeString("File_Bytes", base64);
                    configXmlWriter.WriteEndElement();
                }

                configXmlWriter.WriteEndElement();
            }

            StoredProcs.ConfigurationFiles_CreateConfigurationFileVersions(
                configFileId,
                configBuffer.ToString(),
                releaseNumber
            ).ExecuteNonQuery();
        }
    }
}
