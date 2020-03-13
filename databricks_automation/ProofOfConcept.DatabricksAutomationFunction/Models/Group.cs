using System.Collections.Generic;

namespace ProofOfConcept.DatabricksAutomationFunction.Models
{
    public class Group
    {
        public Group()
        {
            this.Users = new List<User>();
        }

        public string Name { get; set; }
        public ICollection<User> Users { get; private set; }
    }
}
