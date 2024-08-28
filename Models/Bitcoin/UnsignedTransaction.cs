using NBitcoin.JsonConverters;
using NBitcoin;

namespace SoftHSMSigner.Models.Bitcoin
{
    public class UnsignedTransaction(UnsignedTransactionRequest request, Network network)
    {
        public string KeyPath { get; set; } = request.KeyPath;
        public BitcoinAddress From { get; set; } = BitcoinAddress.Create(request.From, network);
        public BitcoinAddress To { get; set; } = BitcoinAddress.Create(request.To, network);
        public Money Amount { get; set; } = Money.Coins(request.Amount);
        public ICoin[] Coins { get; set; } = request.Coins.Select(coin => Serializer.ToObject<ICoin>(coin, network)).ToArray();
        public FeeRate FeeRate { get; set; } = new FeeRate(Money.Satoshis(request.FeeRate));
    }

    public class UnsignedTransactionRequest
    {
        public required string KeyPath { get; set; }
        public required string From { get; set; }
        public required string To { get; set; }
        public decimal Amount { get; set; }
        public required List<string> Coins { get; set; }
        public decimal FeeRate { get; set; }
    }
}
