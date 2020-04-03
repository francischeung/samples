using System;
using System.Collections.Generic;
using System.Dynamic;
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

        private async Task DeleteUserAsync(string userName)
        {
            var response = await httpClient.DeleteAsync($"/api/2.0/preview/scim/v2/Users/{userName}");
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

        public async Task RemoveOrphanUsersAsync(ICollection<AADUser> aadUsers, ICollection<dynamic> databricksUsers)
        {
            foreach (var databricksUser in databricksUsers)
            {
                if (aadUsers.FirstOrDefault(u => u.Id == (string)databricksUser.userName) == null)
                {
                    await DeleteUserAsync((string)databricksUser.id);
                }
            }
        }

        public ICollection<AADUser> GetFlatUserList(ICollection<AADGroup> aadGroups)
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

        public async Task<ICollection<dynamic>> GetDatabricksUsersAsync()
        {
            var response = await httpClient.GetAsync($"/api/2.0/preview/scim/v2/Users");

            var listUsersJson = await response.Content.ReadAsStringAsync();
            dynamic listUsers = JObject.Parse(listUsersJson);

            var databricksUserList = new List<dynamic>();
            if (listUsers.Resources != null)
            {
                foreach (var user in listUsers.Resources)
                {
                    databricksUserList.Add(user);
                }
            }

            return databricksUserList;
        }

        public async Task RemoveUserLevelEntitlementsAsync(ICollection<dynamic> databricksUsers)
        {
            foreach (var user in databricksUsers)
            {
                if (user.entitlements != null)
                {
                    var content = new
                    {
                        schemas = new string[] { "urn:ietf:params:scim:api:messages:2.0:PatchOp" },
                        Operations = new Operation[] { new Operation() { op = "remove", path = "entitlements" } },
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
                    jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/scim+json"); 
                    var response = await httpClient.PatchAsync($"/api/2.0/preview/scim/v2/Users/{(string)user.id}", jsonContent);
                }
            }
        }
    }

    public class Operation
    {
        public string op { get; set; }
        public string path { get; set; }
    }
}
