using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure.Authentication;
using Newtonsoft.Json;

namespace LogAnalyticsToEventHub
{
    public class Function1
    {
        private readonly IConfiguration configuration;

        private static EventHubClient eventHubClient;

        public Function1(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("Function1")]
        public async Task Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var workspaceId = configuration["WorkspaceId"];
            var clientId = configuration["ClientId"];
            var clientSecret = configuration["ClientSecret"];
            var tenantId = configuration["TenantId"];
            var kustoQuery = configuration["KustoQuery"];

            var authEndpoint = "https://login.microsoftonline.com";
            var tokenAudience = "https://api.loganalytics.io/";

            var adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(authEndpoint),
                TokenAudience = new Uri(tokenAudience),
                ValidateAuthority = true
            };

            var creds = ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, clientSecret, adSettings).GetAwaiter().GetResult();

            var client = new OperationalInsightsDataClient(creds);
            client.WorkspaceId = workspaceId;

            var queryResult = client.Query(kustoQuery);

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(configuration["EventHubConnectionString"])
            {
                EntityPath = configuration["EventHubName"]
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            foreach (var result in queryResult.Results)
            {
                //Process the results by sending to Event Hub
                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result))));
            }
            await eventHubClient.CloseAsync();
        }
    }
}
