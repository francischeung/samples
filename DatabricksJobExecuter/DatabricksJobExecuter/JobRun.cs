using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DatabricksJobExecuter
{
    public class JobRun
    {
        [JsonProperty("run_id")]
        public int RunID { get; set; }
        
        [JsonProperty("number_in_job")]
        public int NumberInJob { get; set; }
    }
}
