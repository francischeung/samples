using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProvisionACLForServicePrin
{
    class Program
    {
        static string databricksInstance = "<your databricks workspace>.azuredatabricks.net";
        static string personalAccessToken = "<your access token>";
        static string servicePrinAppId = "<your service principal application id>";

        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", personalAccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/scim+json"));

            var currentServiceprincipals = await httpClient.GetStringAsync($"https://{databricksInstance}/api/2.0/preview/scim/v2/ServicePrincipals");
            Console.WriteLine(currentServiceprincipals);

            //Create Service Principal in Databricks workspace and put in admins group
            var content = new
            {
                schemas = new string[] { "urn:ietf:params:scim:schemas:core:2.0:ServicePrincipal" },
                applicationId = servicePrinAppId,
                groups = new ValueClass[] { new ValueClass() { value = "admins"} }
            };

            var jsonContent = new StringContent(JsonConvert.SerializeObject(content));
            var response = await httpClient.PostAsync($"https://{databricksInstance}/api/2.0/preview/scim/v2/ServicePrincipals", jsonContent);
            response.EnsureSuccessStatusCode();

            //It appears that you must manually create an Azure Key Vault-backed secret scope:
            //https://docs.microsoft.com/en-us/azure/databricks/security/secrets/secret-scopes#--create-an-azure-key-vault-backed-secret-scope

            var secretScopes = await httpClient.GetStringAsync($"https://{databricksInstance}/api/2.0/secrets/scopes/list");
            Console.WriteLine(secretScopes);

            Console.ReadLine();

            var secrets = await httpClient.GetStringAsync($"https://{databricksInstance}/api/2.0/secrets/list?scope=testsecretscope");
            Console.WriteLine(secrets);

            Console.ReadLine();

            var currentSecretACLs = await httpClient.GetStringAsync($"https://{databricksInstance}/api/2.0/secrets/acls/list?scope=testsecretscope");
            Console.WriteLine(currentSecretACLs);

            Console.ReadLine();

            //Give Service Prinicipal READ access to secret scope
            //https://docs.microsoft.com/en-us/azure/databricks/dev-tools/api/latest/secrets

            //Create Service Principal in Databricks workspace and put in admins group
            var secretScopeACLContent = new
            {
                scope = "testsecretscope",
                principal = servicePrinAppId,
                permission = "READ"
            };

            var jsonContent2 = new StringContent(JsonConvert.SerializeObject(secretScopeACLContent));
            var response2 = await httpClient.PostAsync($"https://{databricksInstance}/api/2.0/secrets/acls/put", jsonContent2);
            response2.EnsureSuccessStatusCode();

            var newSecretACLs = await httpClient.GetStringAsync($"https://{databricksInstance}/api/2.0/secrets/acls/list?scope=testsecretscope");
            Console.WriteLine(newSecretACLs);

            Console.ReadLine();

        }

        public class ValueClass
        {
            public string value { get; set; }
        }

    }


}
