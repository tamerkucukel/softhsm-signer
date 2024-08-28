using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using SoftHSMSigner.Models.Bitcoin;
using SoftHSMSigner.Services;
using System.Text.Json;

namespace SoftHSMSigner.Controllers
{
    [Route("api/softhsm/bitcoin")]
    [ApiController]
    public class BitcoinController : ControllerBase
    {
        [HttpPost("{network}/transaction/sign")]
        public IActionResult SignTransaction([FromBody] UnsignedTransactionRequest request, string network)
        {
            try
            {
                Network? selectedNetwork = Network.GetNetwork(network) ?? throw new Exception("Network should be specified correctly.");
                var unsignedTransaction = new UnsignedTransaction(request, selectedNetwork);
                ExtKey extendedPrivateKey = SoftHSMService.GetExtKey(unsignedTransaction.KeyPath);
                var builder = selectedNetwork.CreateTransactionBuilder();
                var transaction = builder
                    .AddCoins(unsignedTransaction.Coins)
                    .AddKeys(extendedPrivateKey.PrivateKey)
                    .Send(unsignedTransaction.To, unsignedTransaction.Amount)
                    .SetChange(unsignedTransaction.From)
                    .SendEstimatedFees(unsignedTransaction.FeeRate)
                    .BuildTransaction(true);

                if (builder.Verify(transaction)) return Ok(transaction.ToHex());

                throw new Exception("An error occured while signing transaction, transaction is not fully signed or not enough fees");
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{network}/address/{type}/{keyPath}")]
        public IActionResult GetAddress(string keyPath, ScriptPubKeyType type, string network)
        {
            try
            {
                Network? selectedNetwork = Network.GetNetwork(network) ?? throw new Exception("Network should be specified correctly.");
                keyPath = Uri.UnescapeDataString(keyPath);
                return Ok(SoftHSMService.GetExtKey(keyPath).GetPublicKey().GetAddress(type, selectedNetwork).ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(ex.Message);
            }
        }
    }
}
