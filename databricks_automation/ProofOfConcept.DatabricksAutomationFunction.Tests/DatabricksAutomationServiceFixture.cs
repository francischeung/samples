using System.Collections.Generic;
using Xunit;
using ProofOfConcept.DatabricksAutomationFunction.Services;
using ProofOfConcept.DatabricksAutomationFunction.Models;
using System.Net.Http;
using Moq;
using Microsoft.Extensions.Configuration;

namespace ProofOfConcept.DatabricksAutomationFunction.Tests
{
    public class DatabricksAutomationServiceFixture
    {
        [Fact]
        public void GetUserList_Returns_DeDupedList()
        {
            var httpClientFactory = new Mock<IHttpClientFactory>();
            httpClientFactory.Setup(m => m.CreateClient()).Returns(new HttpClient());

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(m => m["DatabricksInstance"]).Returns("testhost");
            
            var target = new DatabricksAutomationService(httpClientFactory.Object, configuration.Object, null);
            
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
