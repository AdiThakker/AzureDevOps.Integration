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
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.Integration
{
    public static class HelperExtensions
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
                return new DevOpsContext(context.Connection, project, gitClient.GetRepositoryAsync(project, repo).Result);
            }
        }
        
        public static DevOpsContext GetBuildDefinitions(this DevOpsContext context)
        {
            using (var buildClient = context.Connection.GetClient<BuildHttpClient>())
            {
                var buildDefinitions = new List<BuildDefinition>();

                // Iterate (as needed) to get the full set of build definitions
                string continuationToken = null;
                do
                {
                    IPagedList<BuildDefinition> buildDefinitionsPage = buildClient.GetFullDefinitionsAsync2(project: context.ProjectName,continuationToken: continuationToken).Result;

                    buildDefinitions.AddRange(buildDefinitionsPage);
                    continuationToken = buildDefinitionsPage.ContinuationToken;
                } while (!String.IsNullOrEmpty(continuationToken));

                return new DevOpsContext(context.Connection, context.ProjectName, context.Repo, buildDefinitions);
            }
        }

        public static IEnumerable<TaskGroup> UpdateBuildDefinitionsWithTagTask(this DevOpsContext context)
        {

            //var process = context.BuildDefinitions
            //    .First().Process;

            //((DesignerProcess)process).Phases.Select(pro => pro.Name).ToList();
            var connection = new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, token));

            using (var taskClient = connection.GetClient<TaskAgentHttpClient>())
            {
                var env = taskClient.GetHashCode();
                var yaml = taskClient.GetTaskGroupsAsync(project).Result;// (project: project.Name).Result;
                return default;
            }
        }        
    }
}
