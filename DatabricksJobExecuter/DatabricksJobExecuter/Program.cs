using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DatabricksJobExecuter
{
    class Program
    {
        static string databricksInstance = "<your databricks workspace>.azuredatabricks.net";
        static string personalAccessToken = "<your access token>";
        static string clusterId = "<you cluster id>";

        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/scim+json"));

            var jobs = await httpClient.GetStringAsync($"https://{databricksInstance}/api/2.0/jobs/list");
            Console.WriteLine(jobs);

            //Create job in Databricks workspace
            var content = new
            {
                name = "Test Job",
                existing_cluster_id = clusterId,
                spark_python_task = new
                {
                    python_file = "dbfs:/FileStore/tables/script.py",
                    parameters = new string[] { }
                },
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
            //var response = await httpClient.PostAsync($"https://{databricksInstance}/api/2.0/jobs/runs/submit", jsonContent);
            var response = await httpClient.PostAsync($"https://{databricksInstance}/api/2.0/jobs/create", jsonContent);
            response.EnsureSuccessStatusCode();

        }
    }
}
