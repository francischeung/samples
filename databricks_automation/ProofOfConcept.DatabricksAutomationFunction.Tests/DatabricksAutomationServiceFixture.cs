using System.Collections.Generic;
using Xunit;
using ProofOfConcept.DatabricksAutomationFunction.Services;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Tests
{
    public class DatabricksAutomationServiceFixture
    {
        [Fact]
        public void GetUserList_Returns_DeDupedList()
        {
            var target = new DatabricksAutomationService();
            var groups = new List<Group>();
            
            var user1 = new User("user1", "user one");
            var user2 = new User("user2", "user two");
            var user3 = new User("user3", "user three");
            
            var group1 = new Group();
            group1.Users.Add(user1);
            group1.Users.Add(user2);

            var group2 = new Group();
            group2.Users.Add(user1);
            group2.Users.Add(user3);

            var users = target.GetUserList(new Group[] { group1, group2 });

            Assert.Equal(3, users.Count);
        }
    }
}
