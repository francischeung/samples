using System.Collections.Generic;

namespace ProofOfConcept.DatabricksAutomationFunction.Models
{
    public class WorkspaceConfiguration
    {
        public string workspace { get; set; }
        public ICollection<GroupConfiguration> groups { get; set; }
    }
}