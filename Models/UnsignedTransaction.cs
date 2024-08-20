namespace SoftHSM_API_NET_8.Models
{
    public class UnsignedBTCTransaction
    {
        public required string txHash { get; set; }
        public required string keyPath { get; set; }
        public required List<string> coins { get; set; }
        public required string network { get; set; }
    }
}
