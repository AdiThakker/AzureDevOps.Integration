﻿using System;
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
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.Integration
{
    public static class DevOpsExtensions
    {
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
            return new DevOpsContext(new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, configuration.GetValue<string>(token))));

        }

        public static DevOpsContext GetConfiguredRepository(this DevOpsContext context)
        {
            using (var gitClient = context.Connection.GetClient<GitHttpClient>())
            {
                var repository = gitClient.GetRepositoryAsync(project, repo).Result;
                var properties = new Dictionary<string, object>();
                properties.Add("project", project);
                properties.Add("repo", repository);
                
                return new DevOpsContext(context.Connection, properties);
            }
        }

        public static DevOpsContext GetLatestBuildDefinition(this DevOpsContext context)
        {
            var buildClient = context.Connection.GetClient<BuildHttpClient>();
            {
                var latestDefinition = buildClient.GetFullDefinitionsAsync(project: project).Result.FirstOrDefault();
                Dictionary<string, object> properties = context.Properties ?? new Dictionary<string, object>();
                properties.Add("latestDefinition", latestDefinition ?? new BuildDefinition() { Name = "Adding Tags Definition" });

                return new DevOpsContext(context.Connection, properties);
            }
        }

        public static BuildDefinition UpdateLatestBuildDefinitionsWithTagTask(this DevOpsContext context)
        {
            var buildDefinition = (BuildDefinition)context.Properties["latestDefinition"];
            var process = buildDefinition?.Process as DesignerProcess;
            if (process == null)
                process = new DesignerProcess();
            var connection = new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, token));


            using (var taskClient = connection.GetClient<TaskAgentHttpClient>())
            {
                var taskGroups = taskClient.GetTaskGroupsAsync(project).Result;

                // Get tag Group
                var taskGroup = taskGroups.First(group => group.Name == "Export-MetaTags");
                var tagTask = taskGroup.Tasks.First();

                // import to the build definition
                var phase = process.Phases.First();
                phase.Steps.Add(new BuildDefinitionStep()
                {
                    DisplayName = taskGroup.Name, // tagTask.DisplayName,
                    AlwaysRun = true, //taskGroup.// tagTask.AlwaysRun,
                    //Condition = tagTask.Condition,
                    //ContinueOnError = tagTask.ContinueOnError,
                    Enabled = tagTask.Enabled,
                    Environment = tagTask.Environment,
                    Inputs = tagTask.Inputs,
                    //RefName = tagTask.ReferenceName,
                    TimeoutInMinutes = tagTask.TimeoutInMinutes,
                    TaskDefinition = new Microsoft.TeamFoundation.Build.WebApi.TaskDefinitionReference()
                    {
                        DefinitionType = "metaTask",
                        Id = taskGroup.Id,

                    }
                });
            }

            var buildClient = connection.GetClient<BuildHttpClient>();
            buildDefinition = buildClient.UpdateDefinitionAsync(buildDefinition).Result;


            return buildDefinition;
        }
    }
}
