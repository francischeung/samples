using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.AspNet.WebHooks;
using Microsoft.AspNet.WebHooks.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SalesforceWebhook
{
    public class SalesforceHandler : WebHookHandler
    {
        static EventHubProducerClient producerClient;

        public override async Task ExecuteAsync(string receiver, WebHookHandlerContext context)
        {
            SalesforceNotifications updates = context.GetDataOrDefault<SalesforceNotifications>();
            
            var connectionString = ConfigurationManager.AppSettings["EHconnectionString"];

            // Create a producer client that you can use to send events to an event hub
            producerClient = new EventHubProducerClient(connectionString, "notifications");

            // Create a batch of events 
            EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            //foreach(var notification in updates.Notifications)
            //{
            var jsonPayload = JsonConvert.SerializeObject(updates);
            
                //logger.LogCritical(jsonPayload);

                if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(jsonPayload))))
                {
                    // if it is too large for the batch
                    throw new Exception($"Event is too large for the batch and cannot be sent.");
                }
            //}

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch);
                Console.WriteLine($"A batch of {updates.Notifications.Count()} events has been published.");
            }
            finally
            {
                await producerClient.DisposeAsync();
            }

            //await Task.CompletedTask;
        }
    }
}