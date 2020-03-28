using System.Collections.Generic;

namespace ProofOfConcept.DatabricksAutomationFunction.Models
{
    public class AADGroup
    {
        public AADGroup()
        {
            this.Users = new List<AADUser>();
        }

        public string Name { get; set; }
        public ICollection<AADUser> Users { get; private set; }
    }
}
