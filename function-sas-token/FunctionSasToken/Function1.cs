using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Sas;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace FunctionSasToken
{
    public class Function1
    {
        private readonly IConfiguration configuration;

        public Function1(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        [FunctionName("Function1")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            //https://docs.microsoft.com/en-us/azure/storage/common/storage-auth-aad-msi

            //// Construct the blob container endpoint from the arguments.
            //string containerEndpoint = string.Format("https://{0}.blob.core.windows.net/{1}",
            //                                            configuration["BlobAccountName"],
            //                                            configuration["BlobContainerName"]);

            //// Get a credential and create a client object for the blob container.
            //BlobContainerClient blobContainerClient = new BlobContainerClient(new Uri(containerEndpoint),
            //                                                                new DefaultAzureCredential());
            //log.LogInformation("after blobContainerClient ctor");

            //var blobClient = blobContainerClient.GetBlobClient(configuration["BlobName"]);
            //log.LogInformation("after GetBlobClient");

            //var download = await blobClient.DownloadAsync();
            //log.LogInformation("after DownloadAsync");

            //StreamReader reader = new StreamReader(download.Value.Content);
            //log.LogInformation("after StreamReader ctor");

            //string downloadText = reader.ReadToEnd();
            //return new OkObjectResult(downloadText);

            //https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-user-delegation-sas-create-dotnet

            // Construct the blob endpoint from the account name.
            string blobEndpoint = string.Format("https://{0}.blob.core.windows.net", configuration["BlobAccountName"]);

            // Create a new Blob service client with Azure AD credentials.
            BlobServiceClient blobServiceClient = new BlobServiceClient(new Uri(blobEndpoint),
                                                                 new DefaultAzureCredential());

            // Get a user delegation key for the Blob service that's valid for seven days.
            // You can use the key to generate any number of shared access signatures over the lifetime of the key.
            UserDelegationKey key = await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                               DateTimeOffset.UtcNow.AddDays(7));

            // Create a SAS token that's valid for one hour.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = configuration["BlobContainerName"],
                BlobName = configuration["BlobName"],
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            // Specify read permissions for the SAS.
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Use the key to get the SAS token.
            string sasToken = sasBuilder.ToSasQueryParameters(key, configuration["BlobAccountName"]).ToString();

            // Construct the full URI, including the SAS token.
            UriBuilder fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = $"{configuration["BlobAccountName"]}.blob.core.windows.net",
                Path = $"{configuration["BlobContainerName"]}/{configuration["BlobName"]}",
                Query = sasToken
            };

            return new OkObjectResult(fullUri.Uri.ToString());
        }
    }
}
