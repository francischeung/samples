using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Newtonsoft.Json;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using System.Collections.Generic;

namespace UserOnboardFunctionVSCode
{
    public static class UserOnboardFunctionVSCode
    {
        [FunctionName("UserOnboardFunctionVSCode")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("In HTTP trigger function UserOnboardFunction processing a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogDebug($"requestBody: {requestBody}");
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var aADGroupName = "groupname";
            var userName = "username@domain.com";

            //Key Vault secrets pulled using App Settings to reference Key Vault
            //https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references
            var clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
            var tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);

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
                throw new ArgumentException($"Can not find matching group: {aADGroupName} in AAD");

            var selectedGroup = page[0];

            log.LogInformation($"Found group in AAD:{selectedGroup.DisplayName} Id:{selectedGroup.Id}");

            var user = await graphClient.Users[userName]
                .Request()
                .GetAsync();
                
            await graphClient.Groups[selectedGroup.Id].Members.References.Request().AddAsync(user);

            return new OkResult();
        }
    }
}
