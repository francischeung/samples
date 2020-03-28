using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProofOfConcept.DatabricksAutomationFunction.Models;

namespace ProofOfConcept.DatabricksAutomationFunction.Services
{
    public class DatabricksAutomationService : IDatabricksAutomationService
    {
        private readonly ILogger<DatabricksAutomationService> log;
        private readonly HttpClient httpClient;

        public DatabricksAutomationService(IHttpClientFactory httpClientFactory,
                                           ILogger<DatabricksAutomationService> log)
        {
            //TODO: Use Token API to generate tokens or fetch from Key Vault

            this.log = log;
            this.httpClient = httpClientFactory.CreateClient("DatabricksInstance");
        }

        public async Task SynchronizeGroupAsync(GroupConfiguration groupConfiguration, AADGroup aadGroup)
        {
            string databricksGroupName;
            if (groupConfiguration.IsAdmin)
            {
                databricksGroupName = "admins";
            }
            else
            {
                databricksGroupName = aadGroup.Name;
            }

            ICollection<string> databricksGroupMembers;
            var response = await httpClient.GetAsync($"/api/2.0/groups/list-members?group_name={databricksGroupName}");

            if (!response.IsSuccessStatusCode)
            {
                //Create group
                await CreateGroupAsync(databricksGroupName, groupConfiguration);
                databricksGroupMembers = new List<string>();
            }
            else
            {
                databricksGroupMembers = await GetGroupMembersAsync(response);
            }

            foreach (var aadUser in aadGroup.Users)
            {
                if (!databricksGroupMembers.Contains(aadUser.Id))
                {
                    await CreateUserAsync(aadUser.Id);
                    await AddUserToGroupAsync(aadUser.Id, databricksGroupName);
                }
            }

            //Remove user if in Databricks group but not in AAD group
            foreach (var databricksUserId in databricksGroupMembers)
            {
                if (aadGroup.Users.FirstOrDefault(u => u.Id == databricksUserId) == null)
                {
                    await RemoveUserFromGroupAsync(databricksUserId, databricksGroupName);
                }
            }

        }

        private async Task AddUserToGroupAsync(string userName, string groupName)
        {
            var content = new
            {
                user_name = userName,
                parent_name = groupName
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/scim+json");
            var response = await httpClient.PostAsync($"/api/2.0/groups/add-member", jsonContent);
            response.EnsureSuccessStatusCode();
        }

        private async Task RemoveUserFromGroupAsync(string userName, string groupName)
        {
            var content = new
            {
                user_name = userName,
                parent_name = groupName
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/scim+json");
            var response = await httpClient.PostAsync($"/api/2.0/groups/remove-member", jsonContent);
            //response.EnsureSuccessStatusCode();
        }

        private async Task CreateUserAsync(string userName)
        {
            var content = new
            {
                schemas = new string[] { "urn:ietf:params:scim:schemas:core:2.0:User" },
                userName = userName,
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/scim+json");
            var response = await httpClient.PostAsync($"/api/2.0/preview/scim/v2/Users", jsonContent);
            //response.EnsureSuccessStatusCode();
        }

        private static async Task<ICollection<string>> GetGroupMembersAsync(HttpResponseMessage response)
        {
            var listMembersJson = await response.Content.ReadAsStringAsync();
            dynamic listMembers = JObject.Parse(listMembersJson);

            var databricksGroupMemberList = new List<string>();
            if (listMembers.members != null)
            {
                foreach (var member in listMembers.members)
                {
                    databricksGroupMemberList.Add((string)member.user_name);
                }
            }

            return databricksGroupMemberList;
        }

        private async Task CreateGroupAsync(string groupName, GroupConfiguration groupConfiguration)
        {
            var content = new
            {
                schemas = new string[] { "urn:ietf:params:scim:schemas:core:2.0:Group" },
                displayName = groupName,
                entitlements = groupConfiguration.Entitlements
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
            jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/scim+json");
            var response = await httpClient.PostAsync($"/api/2.0/preview/scim/v2/Groups", jsonContent);
            response.EnsureSuccessStatusCode();
        }

        public async Task RemoveOrphanUsersAsync(ICollection<AADUser> users, WorkspaceConfiguration workspaceConfiguration)
        {
            //throw new NotImplementedException();
        }

        public ICollection<AADUser> GetUserList(ICollection<AADGroup> aadGroups)
        {
            var userDictionary = new Dictionary<string, AADUser>();

            foreach (var aadGroup in aadGroups)
            {
                foreach (var user in aadGroup.Users)
                {
                    userDictionary[user.Id] = user;
                }
            }

            return userDictionary.Values;
        }

    }
}
