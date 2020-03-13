using ProofOfConcept.DatabricksAutomationFunction.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public class DatabricksAutomationService : IDatabricksAutomationService
    {
        public void SynchronizeGroup(GroupConfiguration groupConfiguration, object group)
        {
            //throw new NotImplementedException();
        }

        public void RemoveOrphanUsers(ICollection<User> users, WorkspaceConfiguration workspaceConfiguration)
        {
            //throw new NotImplementedException();
        }

        public ICollection<User> GetUserList(ICollection<Group> groups)
        {
            var userDictionary = new Dictionary<string, User>();

            foreach (var group in groups)
            {
                foreach (var user in group.Users)
                {
                    userDictionary[user.Id] = user;
                }
            }

            return userDictionary.Values;
        }

    }
}
