using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.Integration.Models
{
    public class DevOpsContext
    {
        public DevOpsContext(VssConnection connection) => Connection = connection;

        public DevOpsContext(VssConnection connection, string projectName, GitRepository repo) : this(connection)
        {
            ProjectName = projectName;
            Repo = repo;
        }

        public string? ProjectName { get; private set; }
        
        public GitRepository? Repo { get; private set; }
        
        public VssConnection Connection { get; private set; }
    }
}
