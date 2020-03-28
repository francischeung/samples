using System.Collections.Generic;
using System.Threading.Tasks;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public interface IDatabricksAutomationService
    {
        Task SynchronizeGroupAsync(GroupConfiguration groupConfiguration, AADGroup group);
        Task RemoveOrphanUsersAsync(ICollection<AADUser> users, WorkspaceConfiguration workspaceConfiguration);
        ICollection<AADUser> GetUserList(ICollection<AADGroup> groups);

    }
}