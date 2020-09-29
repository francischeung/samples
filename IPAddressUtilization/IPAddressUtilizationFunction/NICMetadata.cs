namespace IPAddressUtilizationFunction
{
    public class NICMetadata
    {
        public NICMetadata(string nicId, string subnetId, string ipAddress)
        {
            NicId = nicId;
            SubnetId = subnetId;
            IpAddress = ipAddress;
        }

        public string SubnetId { get; set; }
        public string NicId { get; set; }
        public string IpAddress { get; set; }
        public double BytesSent { get; set; }
        public double BytesReceived { get; set; }
    }
}
