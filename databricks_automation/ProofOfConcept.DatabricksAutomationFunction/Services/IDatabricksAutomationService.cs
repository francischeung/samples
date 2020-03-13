using System.Collections.Generic;
using System.Threading.Tasks;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public interface IDatabricksAutomationService
    {
        Task SynchronizeGroupAsync(GroupConfiguration groupConfiguration, Group group);
        Task RemoveOrphanUsersAsync(ICollection<User> users, WorkspaceConfiguration workspaceConfiguration);
        ICollection<User> GetUserList(ICollection<Group> groups);

    }
}