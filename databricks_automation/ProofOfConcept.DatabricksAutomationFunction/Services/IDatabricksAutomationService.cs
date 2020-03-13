using ProofOfConcept.DatabricksAutomationFunction.Models;
using System.Collections.Generic;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public interface IDatabricksAutomationService
    {
        void SynchronizeGroup(GroupConfiguration groupConfiguration, object group);
        void RemoveOrphanUsers(ICollection<User> users, WorkspaceConfiguration workspaceConfiguration);
        ICollection<User> GetUserList(ICollection<Group> groups);

    }
}