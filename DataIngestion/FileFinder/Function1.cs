using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace FileFinder
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var sitesSetting = Environment.GetEnvironmentVariable("SFTPSites", EnvironmentVariableTarget.Process);
            var sites = sitesSetting.Split(',');

            List<string> sftpFiles = new List<string>();

            foreach (var site in sites)
            {
                log.LogInformation("Processing site: {Site}", site);

                await GetFilesAsync(site, sftpFiles);
            }

            var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnection", EnvironmentVariableTarget.Process);
            var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName", EnvironmentVariableTarget.Process);
            var queueClient = new QueueClient(serviceBusConnectionString, queueName);
            
            foreach (var sftpFile in sftpFiles)
            {
                var message = new Message(Encoding.UTF8.GetBytes(sftpFile));

                // Send the message to the queue
                await queueClient.SendAsync(message);
            }
        }

        private async Task GetFilesAsync(string site, List<string> sftpFiles)
        {
            for (int i = 0; i < 2; i++)
            {
                var fileName = Guid.NewGuid().ToString();
                sftpFiles.Add($"http://{site}/{fileName}.csv");
            }
            await Task.Delay(0);
        }
    }
}
