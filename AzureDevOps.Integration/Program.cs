using AzureDevOps.Integration;
using Microsoft.Extensions.Configuration;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, Azure DevOps!");


var builder  = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<Program>();
var buildDefinition = builder
    .Build()
    .ConnectToAzureDevOps()
    .GetConfiguredRepository()
    //.GetLatestBuildDefinition()
    //.UpdateLatestBuildDefinitionsWithTagTask()
    .GetLatestReleaseDefinition()
    .UpdateLatestReleaseDefinitionsWithTagTask();


    //.UpdateLatestReleaseDefinitionsWithTagTask();

Console.ReadKey();



