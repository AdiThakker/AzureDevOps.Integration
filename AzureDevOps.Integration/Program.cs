using AzureDevOps.Integration;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, Azure DevOps!");


var builder  = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<Program>();
var builds = builder
                .Build()
                .ConnectToAzureDevOps()
                .GetConfiguredRepository()
                .GetBuildDefinitions();

builds.ToList().ForEach(build => Console.WriteLine($"Build Definition: {build.Name}"));
Console.ReadKey();



