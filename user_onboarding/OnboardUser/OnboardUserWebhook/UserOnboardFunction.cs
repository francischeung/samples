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

namespace OnboardUserWebhook
{
    public static class UserOnboardFunction
    {
        [FunctionName("UserOnboardFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("In HTTP trigger function UserOnboardFunction processing a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogDebug($"requestBody: {requestBody}");
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            //Key Vault secrets pulled using App Settings to reference Key Vault
            //https://docs.microsoft.com/en-us/azure/app-service/app-service-key-vault-references
            var clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
            var tenantId = Environment.GetEnvironmentVariable("TenantId", EnvironmentVariableTarget.Process);

            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var authenticated = Azure
                    .Configure()
                    .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                    .Authenticate(credentials);

            var user = await authenticated.ActiveDirectoryUsers.GetByNameAsync(data.username);
            log.LogDebug($"user: {user?.Name}");

            var group = await authenticated.ActiveDirectoryGroups.GetByNameAsync(data.groupname);
            log.LogDebug($"group: {group?.Name}");

            await group.Update()
                .WithMember(user)
                .ApplyAsync();

            return new OkResult();
        }
    }
}
