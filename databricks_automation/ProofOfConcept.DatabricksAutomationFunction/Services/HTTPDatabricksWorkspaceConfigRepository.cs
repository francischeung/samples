using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public class HTTPDatabricksWorkspaceConfigRepository : IDatabricksWorkspaceConfigRepository
    {
        private readonly ILogger<HTTPDatabricksWorkspaceConfigRepository> log;
        private readonly HttpClient httpClient;

        public HTTPDatabricksWorkspaceConfigRepository(ILogger<HTTPDatabricksWorkspaceConfigRepository> log,
                                                       IHttpClientFactory httpClientFactory)
        {
            this.log = log;
            this.httpClient = httpClientFactory.CreateClient("DatabricksWorkspaceConfig");
        }

        public async Task<ICollection<WorkspaceConfiguration>> GetWorkspaceConfigurationsAsync()
        {
            var workspaceConfigJson = await httpClient.GetStringAsync(string.Empty);
            return JsonConvert.DeserializeObject<ICollection<WorkspaceConfiguration>>(workspaceConfigJson);
        }
    }
}
