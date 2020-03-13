using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public class DatabricksWorkspaceLocalConfigRepository : IDatabricksWorkspaceConfigRepository
    {
        private readonly ILogger<DatabricksWorkspaceLocalConfigRepository> log;

        public DatabricksWorkspaceLocalConfigRepository(ILogger<DatabricksWorkspaceLocalConfigRepository> log)
        {
            this.log = log;
        }

        public async Task<ICollection<WorkspaceConfiguration>> GetWorkspaceConfigurationsAsync(ExecutionContext context)
        {
            //TODO: replace this with a GET request to GitHub/GitLab

            var configurationFile = Path.Combine(context.FunctionAppDirectory, "databricks_automation.json");
            log.LogInformation($"configuration file: {configurationFile}");

            var workspaceConfigJson = await File.ReadAllTextAsync(configurationFile);
            return JsonConvert.DeserializeObject<ICollection<WorkspaceConfiguration>>(workspaceConfigJson);
        }
    }
}
