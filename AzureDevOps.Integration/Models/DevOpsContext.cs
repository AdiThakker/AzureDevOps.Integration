using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace AzureDevOps.Integration.Models
{
    public class DevOpsContext
    {
        public DevOpsContext(VssConnection connection) => Connection = connection ?? throw new ArgumentNullException(nameof(connection));

        public DevOpsContext(VssConnection connection, Dictionary<string, object> properties) : this(connection) => Properties = properties ?? throw new ArgumentNullException(nameof(properties));

        public VssConnection Connection { get; private set; }
        
        public Dictionary<string, object>? Properties { get; private set; }
    }
}
