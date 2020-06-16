using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace DatabricksJobExecuter
{
    public class JobRunOutput
    {
        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
        
        [JsonProperty("notebook_output")]
        public Output Output { get; set; }
    }

    public class Metadata
    {
        public State State { get; set; }
    }

    public class State
    {
        [JsonProperty("life_cycle_state")]
        public string LifeCycleState { get; set; }
    }

    public class Output
    {
        [JsonProperty("result")]
        public string Result { get; set; }
    }
}

