using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.OperationalInsights;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace LogAnalyticsToEventHub
{
    public class LogAnalytics2EventHub
    {
        private const string LastLogTableName = "LastLogTimeTable";
        private const string LastLogTableEntityValueName = "LastLogTimeValue";
        private const string PartitionKey = "MyPartitionKey";
        private const string RowKey = "MyRowKey";
        private readonly IConfiguration configuration;
        private static string lastLogTime;
        private static EventHubClient eventHubClient;

        public LogAnalytics2EventHub(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("LogAnalytics2EventHub")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"LogAnalytics2EventHub: C# Timer trigger function executed at: {DateTime.Now}");

            var workspaceId = configuration["WorkspaceId"];
            var clientId = configuration["ClientId"];
            var clientSecret = configuration["ClientSecret"];
            var tenantId = configuration["TenantId"];
            var kustoQuery = configuration["KustoQuery"];

            var storageAccount = CloudStorageAccount.Parse(configuration["AzureWebJobsStorage"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference(LastLogTableName);

            if (await table.CreateIfNotExistsAsync())
            {
                log.LogInformation("Created Table named: {LastLogTableName}", LastLogTableName);
            }

            if (lastLogTime == null)
            {
                var tableResult = await table.ExecuteAsync(TableOperation.Retrieve(PartitionKey, RowKey));
                if (tableResult?.Result != null)
                {
                    var entity = (DynamicTableEntity)tableResult.Result;
                    var savedLogTime = entity.Properties[LastLogTableEntityValueName].StringValue;

                    //Use value saved in Table Storage if it is not older than 1 day.
                    if (DateTime.Parse(savedLogTime) > DateTime.UtcNow.AddDays(-1))
                    {
                        lastLogTime = savedLogTime;
                    }
                }
                else
                {
                    lastLogTime = DateTime.UtcNow.AddDays(-1).ToString("s");
                }
            }
            var authEndpoint = "https://login.microsoftonline.com";
            var tokenAudience = "https://api.loganalytics.io/";


            var adSettings = new ActiveDirectoryServiceSettings
            {
                AuthenticationEndpoint = new Uri(authEndpoint),
                TokenAudience = new Uri(tokenAudience),
                ValidateAuthority = true
            };

            var creds = ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, clientSecret, adSettings).GetAwaiter().GetResult();
            log.LogInformation("LogAnalytics2EventHub: Logged In");

            var client = new OperationalInsightsDataClient(creds);
            client.WorkspaceId = workspaceId;

            kustoQuery = $"{kustoQuery} | where TimeGenerated > datetime({lastLogTime}) | order by TimeGenerated asc";
            log.LogInformation("LogAnalytics2EventHub: About to run kusto query: {kustoQuery}", kustoQuery);

            var queryResult = client.Query(kustoQuery);

            log.LogInformation("LogAnalytics2EventHub: Ran kusto query");

            var connectionStringBuilder = new EventHubsConnectionStringBuilder(configuration["EventHubConnectionString"])
            {
                EntityPath = configuration["EventHubName"]
            };

            eventHubClient = EventHubClient.CreateFromConnectionString(connectionStringBuilder.ToString());

            log.LogInformation("LogAnalytics2EventHub: About to send to Event Hub");

            var count = 0;
            foreach (var result in queryResult.Results)
            {
                //Process the results by sending to Event Hub
                await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(result))));
                count++;
                lastLogTime = result["TimeGenerated"];
                if (count % 100 == 0)
                {
                    await UpdateLastLogTimeAsync(lastLogTime, table);
                }
            }

            //Store last log time
            await UpdateLastLogTimeAsync(lastLogTime, table);

            await eventHubClient.CloseAsync();
            log.LogInformation("LogAnalytics2EventHub: Sent {count} messages to Event Hub", count);
        }

        private static async Task UpdateLastLogTimeAsync(string lastLogTime, CloudTable table)
        {
            var tableEntity = new DynamicTableEntity(PartitionKey, RowKey);
            tableEntity.Properties[LastLogTableEntityValueName] = new EntityProperty(lastLogTime);
            await table.ExecuteAsync(TableOperation.InsertOrReplace(tableEntity));
        }
    }
}
