using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public class UserGroupRepository : IUserGroupRepository
    {
        private readonly ILogger<UserGroupRepository> log;
        private readonly string clientId;
        private readonly string tenantId;
        private readonly string clientSecret;

        public UserGroupRepository(IConfiguration configuration, ILogger<UserGroupRepository> log)
        {
            this.log = log;
            this.clientId = configuration["ClientId"];
            this.tenantId = configuration["TenantId"];
            this.clientSecret = configuration["ClientSecret"];
        }

        public async Task<Models.AADGroup> GetGroupMembershipAsync(string aADGroupName)
        {
            //TODO: use MSI to access Graph API
            //https://csharp.hotexamples.com/examples/-/GraphServiceClient/-/php-graphserviceclient-class-examples.html

            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tenantId)
                .WithClientSecret(clientSecret)
                .Build();

            var authProvider = new ClientCredentialProvider(confidentialClientApplication);
            var graphClient = new GraphServiceClient(authProvider);

            var options = new List<QueryOption>
                {
                    new QueryOption("$filter",
                      $"displayName eq '{aADGroupName}'")
                };

            var page = await graphClient.Groups
                .Request(options)
                .GetAsync();

            if (page == null || page.Count != 1)
                throw new ArgumentException("Can not find matching group in AAD");

            var selectedGroup = page[0];

            log.LogInformation($"Found group in AAD:{selectedGroup.DisplayName} Id:{selectedGroup.Id}");

            var members = await graphClient.Groups[selectedGroup.Id].Members
                .Request()
                .GetAsync();

            var group = new Models.AADGroup() { Name = aADGroupName };

            foreach (var member in members)
            {
                var user = await graphClient.Users[member.Id]
                    .Request()
                    .GetAsync();

                if (user == null) throw new ArgumentNullException($"User not found: {member.Id}");

                group.Users.Add(new Models.AADUser(user.UserPrincipalName, user.DisplayName));
            }

            return group;
        }
    }
}
