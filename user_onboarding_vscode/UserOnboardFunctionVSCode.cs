using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Newtonsoft.Json;
using Microsoft.CSharp.RuntimeBinder;

namespace UserOnboardFunctionVSCode
{
    public class UserOnboardFunctionVSCode
    {
        private readonly HttpClient httpClient;
        private readonly string clientId;
        private readonly string tenantId;
        private readonly string clientSecret;

        public UserOnboardFunctionVSCode(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            //Key Vault secrets pulled using App Settings to reference Key Vault
            //https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references
            this.clientId = configuration["ClientId"];
            this.clientSecret = configuration["ClientSecret"];
            this.tenantId = configuration["TenantId"];

            this.httpClient = httpClientFactory.CreateClient("RoleGroupMapping");    
        }

        [FunctionName("UserOnboardFunctionVSCode")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("In HTTP trigger function UserOnboardFunction processing a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogDebug($"requestBody: {requestBody}");
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            string userName;
            string role;

            try
            {
                userName = $"{data.ntid.ToString()}@T-Mobile.com";
                role = data.role.ToString();
            }
            catch (RuntimeBinderException ex)
            {
                throw new ArgumentException("Request missing value.", ex);
            } 

            var aADGroups = await GetAADGroupsForRoleAsync(role);
            
            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tenantId)
                .WithClientSecret(clientSecret)
                .Build();

            var authProvider = new ClientCredentialProvider(confidentialClientApplication);
            var graphClient = new GraphServiceClient(authProvider);

            var user = await graphClient.Users[userName]
                .Request()
                .GetAsync();

            foreach (var aADGroupName in aADGroups)
            {
                var options = new List<QueryOption>
                    {
                        new QueryOption("$filter",
                        $"displayName eq '{aADGroupName}'")
                    };

                var page = await graphClient.Groups
                    .Request(options)
                    .GetAsync();

                if (page == null || page.Count != 1)
                    throw new ArgumentException($"Can not find matching group: {aADGroupName} in AAD");

                var selectedGroup = page[0];

                log.LogInformation($"Found group in AAD:{selectedGroup.DisplayName} Id:{selectedGroup.Id}");
                    
                await graphClient.Groups[selectedGroup.Id].Members.References.Request().AddAsync(user);                
            }

            return new OkResult();
        }

        private async Task<IList<string>> GetAADGroupsForRoleAsync(string roleName){
            var roleGroupJson = await httpClient.GetStringAsync(string.Empty);
            var roleGroupDictionary = JsonConvert.DeserializeObject<Dictionary<string, IList<string>>>(roleGroupJson);

            return roleGroupDictionary[roleName];
        }
    }
}
