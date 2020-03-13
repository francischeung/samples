namespace ProofOfConcept.DatabricksAutomationFunction.Models
{
    public class User
    {
        public User(string id, string name)
        {
            this.Id = id;
            this.Name = name;
        }

        public string Id { get; set; }
        public string Name { get; set; }
    }
}
