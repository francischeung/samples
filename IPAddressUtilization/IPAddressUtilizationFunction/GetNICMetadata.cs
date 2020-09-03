using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IPAddressUtilizationFunction
{
    public class GetNICMetadata
    {
        private readonly IConfiguration configuration;
        private const string azureManagementDomain = "https://management.azure.com";

        public GetNICMetadata(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("GetNICMetadata")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"GetNICMetadata C# Timer trigger function executed at: {DateTime.Now}");

            //https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet#obtain-tokens-for-azure-resources

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(azureManagementDomain);

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(azureManagementDomain);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var nicDictionary = new Dictionary<string, NICMetadata>();

            var subscriptionIds = configuration["SubscriptionIds"].Split(',');

            foreach (var subscriptionId in subscriptionIds)
            {
                //Get all network interfaces in this subscription?
                //https://docs.microsoft.com/en-us/rest/api/virtualnetwork/networkinterfaces/listall
                var response = await httpClient.GetAsync($"/subscriptions/{subscriptionId}/providers/Microsoft.Network/networkInterfaces?api-version=2020-05-01");
                response.EnsureSuccessStatusCode();

                var networkInterfacesJson = await response.Content.ReadAsStringAsync();
                dynamic networkInterfaces = JObject.Parse(networkInterfacesJson);


                if (networkInterfaces.value != null)
                {
                    foreach (var networkInterface in networkInterfaces.value)
                    {
                        foreach (var ipConfiguration in networkInterface.properties.ipConfigurations)
                        {
                            var ipMetadata = new NICMetadata((string)ipConfiguration.id, (string)ipConfiguration.properties.subnet.id, (string)ipConfiguration.properties.privateIPAddress);
                            nicDictionary[(string)ipConfiguration.id] = ipMetadata;
                        }
                    }
                }
            }

            //Send data to Log Analytics
            // https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api#sample-requests
            var sharedKey = configuration["LogAnalyticsWorkspaceSharedKey"];
            var workspaceId = configuration["LogAnalyticsWorkspaceId"];

            foreach (var ipMetadata in nicDictionary.Values)
            {
                var json = JsonConvert.SerializeObject(ipMetadata);

                // Create a hash for the API signature
                var datestring = DateTime.UtcNow.ToString("r");
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                string stringToHash = "POST\n" + jsonBytes.Length + "\napplication/json\n" + "x-ms-date:" + datestring + "\n/api/logs";
                string hashedString = BuildSignature(stringToHash, sharedKey);
                string signature = "SharedKey " + workspaceId + ":" + hashedString;

                PostData(workspaceId, signature, datestring, json, log);
            }
        }

        // Build the API signature
        public static string BuildSignature(string message, string secret)
        {
            var encoding = new ASCIIEncoding();
            byte[] keyByte = Convert.FromBase64String(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hash = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Send a request to the POST API endpoint
        public static void PostData(string workspaceId, string signature, string date, string json, ILogger log)
        {
            try
            {
                string url = "https://" + workspaceId + ".ods.opinsights.azure.com/api/logs?api-version=2016-04-01";

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", "NICMetadataLogs");
                client.DefaultRequestHeaders.Add("Authorization", signature);
                client.DefaultRequestHeaders.Add("x-ms-date", date);
                client.DefaultRequestHeaders.Add("time-generated-field", "");

                HttpContent httpContent = new StringContent(json, Encoding.UTF8);
                httpContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                Task<HttpResponseMessage> response = client.PostAsync(new Uri(url), httpContent);

                HttpContent responseContent = response.Result.Content;
                string result = responseContent.ReadAsStringAsync().Result;
                log.LogInformation("Return Result: {Result}", result);
            }
            catch (Exception excep)
            {
                log.LogError("API Post Exception: {Exception}", excep);
            }
        }
    }
}
