using AzureDevOps.Integration;
using Microsoft.Extensions.Configuration;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, Azure DevOps!");


var builder  = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<Program>();


var devOps = builder
                .Build()
                .ConnectToAzureDevOps()
                .GetConfiguredRepository();

// update build definition
devOps
    .GetLatestBuildDefinition()
    .UpdateLatestBuildDefinitionsWithTagTask();

// update release definition
devOps
    .GetLatestReleaseDefinition()
    .UpdateLatestReleaseDefinitionsWithTagTask();

Console.WriteLine("Definitions Updated!");
Console.ReadKey();



