using System.Collections.Generic;

namespace NotificationReceiver
{
    internal class NotificationPayload
    {
        public string SessionId { get; set; }
        public IList<IDictionary<string, string>> Notifications { get; set; }
    }
}
