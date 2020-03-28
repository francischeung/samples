using Microsoft.Azure.WebJobs;
using ProofOfConcept.DatabricksAutomationFunction.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public interface IDatabricksWorkspaceConfigRepository
    {
        Task<ICollection<WorkspaceConfiguration>> GetWorkspaceConfigurationsAsync();
    }
}