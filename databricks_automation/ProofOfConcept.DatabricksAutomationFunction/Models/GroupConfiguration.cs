using System.Collections.Generic;

namespace ProofOfConcept.DatabricksAutomationFunction.Models
{
    public class GroupConfiguration
    {
        public string AADGroupName { get; set; }
        public bool IsAdmin { get; set; }
        public ICollection<EntitlementConfiguration> Entitlements { get; set; }
    }
}
