using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProofOfConcept.DatabricksAutomationFunction.Services;
using System;
using System.Net.Http.Headers;

[assembly: FunctionsStartup(typeof(ProofOfConcept.DatabricksAutomationFunction.Startup))]

namespace ProofOfConcept.DatabricksAutomationFunction
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();

            builder.Services.AddHttpClient("DatabricksWorkspaceConfig", c => 
            {
                c.BaseAddress = new Uri(configuration["DatabricksWorkspaceConfigUri"]);
            });

            builder.Services.AddHttpClient("AADAuthTokenService", c =>
            {
                c.BaseAddress = new Uri("https://login.microsoftonline.com/" + configuration["TenantId"] + "/oauth2/token");
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            builder.Services.AddHttpClient("DatabricksAPI", c => 
            {
                c.BaseAddress = new Uri(configuration["AzureDatabricksBaseAddress"]);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/scim+json"));

            });
            builder.Services.AddSingleton<IUserGroupRepository, UserGroupRepository>();
            builder.Services.AddSingleton<IDatabricksWorkspaceConfigRepository, HTTPDatabricksWorkspaceConfigRepository>();
            builder.Services.AddSingleton<IDatabricksAutomationService, DatabricksAutomationService>();
        }
    }
}