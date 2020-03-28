namespace ProofOfConcept.DatabricksAutomationFunction.Models
{
    public class AADUser
    {
        public AADUser(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}
