using System;
using System.Collections.Generic;
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

        public async Task SynchronizeGroupAsync(GroupConfiguration groupConfiguration, Group group)
        {
            string groupName;
            if (groupConfiguration.IsAdmin)
            {
                groupName = "admins";
            }
            else
            {
                groupName = group.Name;
            }

            var response = await httpClient.GetAsync($"/api/2.0/groups/list-members?group_name={groupName}");
            
            if(!response.IsSuccessStatusCode)
            {
                //Create group
                await CreateGroupAsync(group.Name, groupConfiguration);
            }
            else
            {
                var databricksGroupMembers = await GetGroupMembersAsync(response);
                foreach (var user in group.Users)
                {
                    //TODO: handle case where user already a member of group but has different entitlements

                    if (!databricksGroupMembers.Contains(user.Id))
                    {
                        //TODO: handle case where user already exists but not in this group
                        await CreateUserAsync(user.Id);
                        await AddUserToGroupAsync(user.Id, groupName);
                    }
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

        public async Task RemoveOrphanUsersAsync(ICollection<User> users, WorkspaceConfiguration workspaceConfiguration)
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
