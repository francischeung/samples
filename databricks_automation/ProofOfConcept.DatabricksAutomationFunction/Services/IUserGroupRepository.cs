using System.Threading.Tasks;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public interface IUserGroupRepository
    {
        Task<AADGroup> GetGroupMembershipAsync(string aADGroupName);
    }
}