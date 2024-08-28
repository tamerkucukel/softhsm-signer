using Microsoft.AspNetCore.Mvc;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3.Accounts;
using SoftHSMSigner.Models.Ethereum;
using SoftHSMSigner.Services;
using System.Text.Json;

namespace SoftHSMSigner.Controllers
{
    [Route("api/softhsm/ethereum")]
    [ApiController]
    public class EthereumController : ControllerBase
    {
        [HttpPost("transaction/sign/{keyPath}")]
        public IActionResult SignTransaction([ModelBinder(typeof(TransactionModelBinder))] [FromBody] TransactionInput unsignedTransaction, string keyPath)
        {
            try
            {
                Console.WriteLine(unsignedTransaction.ToString());
                var account = new Account(SoftHSMService.GetExtKey(Uri.UnescapeDataString(keyPath)).PrivateKey.ToHex(), unsignedTransaction.ChainId);
                var signedTransaction = new AccountOfflineTransactionSigner().SignTransaction(account, unsignedTransaction);
                return Ok(signedTransaction);
            }
            catch (JsonException ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine (ex.ToString());
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("address/{keyPath}")]
        public IActionResult GetAddress(string keyPath)
        {
            try
            {
                keyPath = Uri.UnescapeDataString(keyPath);
                var account = new Account(SoftHSMService.GetExtKey(keyPath).PrivateKey.ToHex());
                return Ok(account.Address);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
