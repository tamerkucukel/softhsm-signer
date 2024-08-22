using Nethereum.RPC.Eth.DTOs;

namespace SoftHSM_API_NET_8.Models
{
    public class UnsignedBTCTransaction
    {
        public required string txHash { get; set; }
        public required string keyPath { get; set; }
        public required List<string> coins { get; set; }
        public required string network { get; set; }
    }

    public class UnsignedETHTransaction
    {
        public required string? Data { get; set; }
        public required string From { get; set; }
        public required string To { get; set; }
        public required string Gas { get; set; }
        public required string GasPrice { get; set; }
        public required string Value { get; set; }
        public required string Nonce { get; set; }
        public required string ChainId { get; set; }
        public required string KeyPath { get; set; }
    }
}
