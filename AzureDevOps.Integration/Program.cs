using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, Azure DevOps!");


var builder  = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddUserSecrets<Program>();

var configuration = builder.Build();

var repo = ConnectRepository(configuration);

Console.WriteLine($"Connected to : {repo.Name}");
Console.ReadKey();

static GitRepository ConnectRepository(IConfigurationRoot? configuration)
{
    var token = configuration?.GetValue<string>("token");
    var url = configuration?.GetValue<string>("url");
    var project = configuration?.GetValue<string>("project");
    var repo = configuration?.GetValue<string>("repo");

    // Create instance of VssConnection using Personal Access Token
    VssConnection connection = new VssConnection(new Uri(url), new VssBasicCredential(string.Empty, configuration.GetValue<string>(token)));

    // Get a GitHttpClient to talk to the Git endpoints
    using (GitHttpClient gitClient = connection.GetClient<GitHttpClient>())
    {
        // Get data about a specific repository
        return gitClient.GetRepositoryAsync(project, repo).Result;
    }
}
