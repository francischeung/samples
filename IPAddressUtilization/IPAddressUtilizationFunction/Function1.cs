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
    public class Function1
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private const string azureManagementDomain = "https://management.azure.com";

        public Function1(HttpClient httpClient, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.configuration = configuration;
        }

        [FunctionName("IPAddressUtilizationFunction")]
        public async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"IPAddressUtilizationFunction C# Timer trigger function executed at: {DateTime.Now}");

            //https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet#obtain-tokens-for-azure-resources

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            string accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(azureManagementDomain);

            var subscriptionId = configuration["SubscriptionId"];

            httpClient.BaseAddress = new Uri(azureManagementDomain);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            //What VNets do are in this subscription?
            var listVNetsResponse = await httpClient.GetAsync($"/subscriptions/{subscriptionId}/providers/Microsoft.Network/virtualNetworks?api-version=2020-05-01");
            listVNetsResponse.EnsureSuccessStatusCode();

            var listVNetsJson = await listVNetsResponse.Content.ReadAsStringAsync();
            dynamic listVNets = JObject.Parse(listVNetsJson);

            var subnetDictionary = new Dictionary<string, SubnetMetadata>();

            if (listVNets.value != null)
            {
                foreach (var vnet in listVNets.value)
                {
                    foreach (var subnet in vnet.properties.subnets)
                    {
                        var subnetMetadata = new SubnetMetadata((string)vnet.name, (string)subnet.name, (string)subnet.id, (string)subnet.properties.addressPrefix);
                        subnetDictionary[(string)subnet.id] = subnetMetadata;
                    }

                    //What is the VNet/subnet address usage?
                    var listVNetUsageResponse = await httpClient.GetAsync($"{vnet.id}/usages?api-version=2020-05-01");
                    listVNetUsageResponse.EnsureSuccessStatusCode();

                    var listVNetUsageJson = await listVNetUsageResponse.Content.ReadAsStringAsync();
                    dynamic listVNetUsage = JObject.Parse(listVNetUsageJson);

                    foreach (var subnetUsage in listVNetUsage.value)
                    {
                        var subnetMetadata = subnetDictionary[(string)subnetUsage.id];
                        subnetMetadata.Size = (int)subnetUsage.limit;
                        subnetMetadata.Used = (int)subnetUsage.currentValue;
                    }
                }
            }

        //Send data to Log Analytics
        // https://docs.microsoft.com/en-us/azure/azure-monitor/platform/data-collector-api#sample-requests
            var sharedKey = configuration["LogAnalyticsWorkspaceSharedKey"];
            var workspaceId = configuration["LogAnalyticsWorkspaceId"];

            foreach (var subnetMetadata in subnetDictionary.Values)
            {
                var json = JsonConvert.SerializeObject(subnetMetadata);

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
            var encoding = new System.Text.ASCIIEncoding();
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

                HttpClient client = new System.Net.Http.HttpClient();
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Log-Type", "IPAddressUtilizationLogs");
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
