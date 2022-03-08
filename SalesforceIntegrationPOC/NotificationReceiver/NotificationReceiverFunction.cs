using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Cache;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NotificationReceiver
{
    public class NotificationReceiverFunction
    {
        [FunctionName("NotificationReceiverFunction")]
        public async Task Run([EventHubTrigger("notifications", Connection = "EHConnection")] EventData[] events, ILogger log)
        {
            var httpClient = new HttpClient();
            var objectApiUri = System.Environment.GetEnvironmentVariable("ObjectApiUri", EnvironmentVariableTarget.Process);
            httpClient.BaseAddress = new Uri(objectApiUri);

            var exceptions = new List<Exception>();

            foreach (EventData eventData in events)
            {
                try
                {
                    string messageBody = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                    var payload = JsonConvert.DeserializeObject<NotificationPayload>(messageBody);
                    if (payload != null && payload.Notifications != null)
                    {
                        foreach (var notification in payload.Notifications)
                        {
                            if (notification["_NotificationType"] == "Apttus_Config2__LineItem__c")
                            {
                                var lineItemAPIObject = new LineItemAPIObject()
                                {
                                    Name = notification["Name"],
                                    ExternalId = notification["Id"]
                                    
                                    //Add data mapping here...
                                };

                                var httpContent = new StringContent(JsonConvert.SerializeObject(lineItemAPIObject));
                                httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                                var response = await httpClient.PostAsync("", httpContent);
                            }
                        }
                    }

                    // Replace these two lines with your processing logic.
                    log.LogInformation($"C# Event Hub trigger function processed a message: {messageBody}");
                    await Task.Yield();
                }
                catch (Exception e)
                {
                    // We need to keep processing the rest of the batch - capture this exception and continue.
                    // Also, consider capturing details of the message that failed processing so it can be processed again later.
                    exceptions.Add(e);
                }
            }

            // Once processing of the batch is complete, if any messages in the batch failed processing throw an exception so that there is a record of the failure.

            if (exceptions.Count > 1)
                throw new AggregateException(exceptions);

            if (exceptions.Count == 1)
                throw exceptions.Single();
        }
    }
}
