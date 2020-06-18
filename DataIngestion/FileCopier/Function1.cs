using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace FileCopier
{
    public class Function1
    {
        [FunctionName("Function1")]
        public async Task Run([ServiceBusTrigger("myqueue", Connection = "ServiceBusConnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            await CopyFileAsync(myQueueItem);
        }

        private async Task CopyFileAsync(string myQueueItem)
        {
            await Task.Delay(0);
        }
    }
}
