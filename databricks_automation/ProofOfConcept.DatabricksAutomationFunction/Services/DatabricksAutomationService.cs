using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
        private const string AzureDatabricksLoginApplicationId = "2ff814a6-3304-4ab8-85cb-cd0e6f879c1d";
        private const string ManagementResourceEndpoint = "https://management.core.windows.net/";
        private readonly ILogger<DatabricksAutomationService> log;
        private readonly HttpClient databricksAPIHttpClient;
        private readonly HttpClient aadAuthTokenServiceHttpClient;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string workspaceResourceId;

        public DatabricksAutomationService(IHttpClientFactory httpClientFactory,
                                           IConfiguration configuration,
                                           ILogger<DatabricksAutomationService> log)
        {
            this.log = log;
            this.databricksAPIHttpClient = httpClientFactory.CreateClient("DatabricksAPI");
            this.aadAuthTokenServiceHttpClient = httpClientFactory.CreateClient("AADAuthTokenService");
            this.clientId = configuration["ClientId"];
            this.clientSecret = configuration["ClientSecret"];
            this.workspaceResourceId = configuration["WorkspaceResourceId"];
        }

        public async Task SynchronizeGroupAsync(GroupConfiguration groupConfiguration, AADGroup aadGroup)
        {
            //Fetch bearer token from AAD
            var loginApplicationAccessToken = await GetAccessTokenAsync(AzureDatabricksLoginApplicationId);
            var azureSpManagementAccessToken = await GetAccessTokenAsync(ManagementResourceEndpoint);

            databricksAPIHttpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", loginApplicationAccessToken);
            SafelyAddDefaultRequestHeader("X-Databricks-Azure-SP-Management-Token", azureSpManagementAccessToken);
            SafelyAddDefaultRequestHeader("X-Databricks-Azure-Workspace-Resource-Id", workspaceResourceId);

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
            var response = await databricksAPIHttpClient.GetAsync($"/api/2.0/groups/list-members?group_name={databricksGroupName}");

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
                    //Don't remove Service Principal
                    if (databricksUserId != this.clientId)
                    {
                        await RemoveUserFromGroupAsync(databricksUserId, databricksGroupName);
                    }
                }
            }

        }

        private void SafelyAddDefaultRequestHeader(string requestHeaderKey, string azureSpManagementAccessToken)
        {
            if (!databricksAPIHttpClient.DefaultRequestHeaders.Contains(requestHeaderKey))
            {
                databricksAPIHttpClient.DefaultRequestHeaders.Add(requestHeaderKey, azureSpManagementAccessToken);
            }
        }

        private async Task<string> GetAccessTokenAsync(string resource)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            request.Content = new StringContent($"grant_type=client_credentials&client_id={clientId}&resource={resource}&client_secret={clientSecret}",
                                                Encoding.UTF8,
                                                "application/x-www-form-urlencoded");
            var authTokenResponse = await aadAuthTokenServiceHttpClient.SendAsync(request);
            var authTokenResponseJson = await authTokenResponse.Content.ReadAsStringAsync();
            dynamic authTokenDynamic = JObject.Parse(authTokenResponseJson);
            return (string)authTokenDynamic.access_token;
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
            var response = await databricksAPIHttpClient.PostAsync($"/api/2.0/groups/add-member", jsonContent);
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
            var response = await databricksAPIHttpClient.PostAsync($"/api/2.0/groups/remove-member", jsonContent);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteUserAsync(string userName)
        {
            var response = await databricksAPIHttpClient.DeleteAsync($"/api/2.0/preview/scim/v2/Users/{userName}");
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
            var response = await databricksAPIHttpClient.PostAsync($"/api/2.0/preview/scim/v2/Users", jsonContent);
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
            var response = await databricksAPIHttpClient.PostAsync($"/api/2.0/preview/scim/v2/Groups", jsonContent);
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
            var response = await databricksAPIHttpClient.GetAsync($"/api/2.0/preview/scim/v2/Users");

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
                    var response = await databricksAPIHttpClient.PatchAsync($"/api/2.0/preview/scim/v2/Users/{(string)user.id}", jsonContent);
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
