namespace IPAddressUtilizationFunction
{
    public class SubnetMetadata
    {
        public SubnetMetadata(string vnetName, string subnetName, string resourceId, string addressPrefix)
        {
            VNetName = vnetName;
            SubnetName = subnetName;
            ResourceId = resourceId;
            AddressPrefix = addressPrefix;
        }

        public string VNetName { get; set; }
        public string SubnetName { get; set; }
        public string ResourceId { get; set; }
        public string AddressPrefix { get; set; }
        public int Size { get; set; }
        public int Used { get; set; }
    }
}
