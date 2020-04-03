using System.Collections.Generic;
using System.Threading.Tasks;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public interface IDatabricksAutomationService
    {
        Task SynchronizeGroupAsync(GroupConfiguration groupConfiguration, AADGroup group);
        Task RemoveOrphanUsersAsync(ICollection<AADUser> aadUsers, ICollection<dynamic> databricksUsers);
        ICollection<AADUser> GetFlatUserList(ICollection<AADGroup> groups);
        Task<ICollection<dynamic>> GetDatabricksUsersAsync();
        Task RemoveUserLevelEntitlementsAsync(ICollection<dynamic> databricksUsers);
    }
}