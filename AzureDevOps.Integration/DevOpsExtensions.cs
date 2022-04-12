using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzureDevOps.Integration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.DistributedTask.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.Integration
{
    public static class DevOpsExtensions
    {
        private const string buildDefinitionKey = "latestBuildDefinition";
        private const string releaseDefinitionKey = "latestReleaseDefinition";
        static string token = string.Empty;
        static string url = string.Empty;
        static string project = string.Empty;
        static string repo = string.Empty;


        public static DevOpsContext ConnectToAzureDevOps(this IConfigurationRoot configuration)
        {
            token = configuration.GetValue<string>("token");
            url = configuration.GetValue<string>("url");
            project = configuration.GetValue<string>("project");
            repo = configuration.GetValue<string>("repo");

            // return instance of VssConnection using Personal Access Token
            return new DevOpsContext(new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, token)));
        }

        public static DevOpsContext GetConfiguredRepository(this DevOpsContext context)
        {
            using var gitClient = context.Connection.GetClient<GitHttpClient>();

            var repository = gitClient.GetRepositoryAsync(project, repo).Result;
            var properties = new Dictionary<string, object>();

            return new DevOpsContext(context.Connection, properties);
        }

        public static DevOpsContext GetLatestBuildDefinition(this DevOpsContext context)
        {
            using var buildClient = context.Connection.GetClient<BuildHttpClient>();
            var latestDefinition = buildClient.GetFullDefinitionsAsync(project: project).Result.FirstOrDefault();
            Dictionary<string, object> properties = context.Properties ?? new Dictionary<string, object>();
            properties.Add(buildDefinitionKey, latestDefinition ?? new BuildDefinition() { Name = "Adding Meta Tags Task" });

            return new DevOpsContext(context.Connection, properties);
        }

        public static BuildDefinition UpdateLatestBuildDefinitionsWithTagTask(this DevOpsContext context)
        {
            var buildDefinition = context.Properties[buildDefinitionKey] as BuildDefinition;
            var process = buildDefinition?.Process as DesignerProcess;
            if (process == null)
                process = new DesignerProcess();

            using var taskClient = context.Connection.GetClient<TaskAgentHttpClient>();
            var taskGroups = taskClient.GetTaskGroupsAsync(project).Result;

            // Get tag Group
            var taskGroup = taskGroups.First(group => group.Name == "Export-MetaTags");
            var tagTask = taskGroup.Tasks.First();

            // import to the build definition
            var phase = process.Phases.First();
            var buildDefinitionStep = new BuildDefinitionStep()
            {
                DisplayName = taskGroup.Name,
                AlwaysRun = tagTask.AlwaysRun,
                Condition = tagTask.Condition,
                ContinueOnError = tagTask.ContinueOnError,
                Enabled = tagTask.Enabled,
                Environment = tagTask.Environment,
                //RefName = taskGroup.ReferenceName,
                TimeoutInMinutes = tagTask.TimeoutInMinutes,
                TaskDefinition = new Microsoft.TeamFoundation.Build.WebApi.TaskDefinitionReference()
                {
                    DefinitionType = "metaTask",
                    Id = taskGroup.Id,
                    VersionSpec = taskGroup.Version
                }
            };

            // set step inputs
            buildDefinitionStep.Inputs = new Dictionary<string, string>();
            taskGroup.Inputs.ToList().ForEach(input => buildDefinitionStep.Inputs.Add(input.Name, @"$(System.DefaultWorkingDirectory)\**\*.csproj"));
            phase.Steps.Add(buildDefinitionStep);

            // Update build definition
            buildDefinition.Comment = "Updated with Export tag task";
            try
            {
                using var buildClient = context.Connection.GetClient<BuildHttpClient>();
                buildDefinition = buildClient.UpdateDefinitionAsync(buildDefinition).Result;
                var steps = buildDefinition.GetProcess<DesignerProcess>().Phases.First().Steps.ToList();
                return buildDefinition;
            }
            catch (AggregateException aex)
            {
                throw new Exception(aex.Flatten().ToString());
            }
        }

        public static DevOpsContext GetLatestReleaseDefinition(this DevOpsContext context)
        {
            using var releaseClient = context.Connection.GetClient<ReleaseHttpClient>();
            var latestDefinition = releaseClient.GetReleaseDefinitionsAsync(project: project, "", ReleaseDefinitionExpands.Environments).Result.FirstOrDefault();
            Dictionary<string, object> properties = context.Properties ?? new Dictionary<string, object>();
            properties.Add(releaseDefinitionKey, latestDefinition ?? new ReleaseDefinition() { Name = "Adding Meta Tags Task" });
            return new DevOpsContext(context.Connection, properties);
        }

        public static ReleaseDefinition UpdateLatestReleaseDefinitionsWithTagTask(this DevOpsContext context)
        {
            // Get release definition
            var releaseDefinition = context.Properties[releaseDefinitionKey] as ReleaseDefinition;

            // retrieve task from task group
            using var taskClient = context.Connection.GetClient<TaskAgentHttpClient>();
            var taskGroups = taskClient.GetTaskGroupsAsync(project).Result;
            var taskGroup = taskGroups.First(group => group.Name == "Update Tags");
            var tagTask = taskGroup.Tasks.First();

            // Get latest release definition by env.
            using var releaseClient = context.Connection.GetClient<ReleaseHttpClient>();
            var releaseDefinitionByEnv = releaseClient.GetReleaseDefinitionAsync(project, releaseDefinition.Environments.First().Id).Result;

            // Get definition steps
            var workflowTasks = releaseDefinitionByEnv.Environments.First().DeployPhases.First().WorkflowTasks;
            var worklflowTask = new WorkflowTask()
            {
                Name = taskGroup.Name,
                Version = taskGroup.Version,
                AlwaysRun = tagTask.AlwaysRun,
                Condition = tagTask.Condition,
                ContinueOnError = tagTask.ContinueOnError,
                Enabled = tagTask.Enabled,
                TimeoutInMinutes = tagTask.TimeoutInMinutes,
                TaskId = taskGroup.Id,
                DefinitionType = "metaTask"
            };
            worklflowTask.Inputs = new Dictionary<string, string>();
            taskGroup.Inputs.ToList().ForEach(input =>
            {
                var value = input.Name switch
                {
                    "AzureSubscription" => "<default subscription>",
                    "resourceGroupName" => "<default resource group name>",
                    "resourceName" => "<default resource name>",
                    "tagsfile" => @"$(System.DefaultWorkingDirectory)\**\Tags.txt",
                    _ => ""
                };
                worklflowTask.Inputs.Add(input.Name, value);
            });
            
            // Update definition
            releaseDefinitionByEnv.Environments.First().DeployPhases.First().WorkflowTasks.Add(worklflowTask);
            releaseDefinitionByEnv.Comment = "Updated with Update tag task";
                
            try
            {
                releaseDefinitionByEnv = releaseClient.UpdateReleaseDefinitionAsync(releaseDefinitionByEnv, project).Result;
            }
            catch (AggregateException aex)
            {
                throw new Exception(aex.Flatten().ToString());
            }
            
            return releaseDefinitionByEnv;
        }
    }
}
