using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.JsonConverters;
using SoftHSM_API_NET_8.Models;
using SoftHSM_API_NET_8.Services;
using System.Text.Json;

namespace SoftHSM_API_NET_8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoftHSMController : ControllerBase
    {
        [HttpPost("transaction/sign")]
        public IActionResult SignTransaction(UnsignedTransaction unsignedtx)
        {
            try
            {
                // Extract txHash and keypath
                if(unsignedtx.txHash == null || unsignedtx.keyPath == null || unsignedtx.coins == null || unsignedtx.network == null)
                {
                    throw new JsonException("txHash, keyPath and coins fields must exist !");
                }

                // Extract and process the list of coins.
                
                List<Coin> coins = [];
                Network? network = Network.GetNetwork(unsignedtx.network);

                foreach (var coinStr in unsignedtx.coins)
                {
                    Coin coin = Serializer.ToObject<Coin>(coinStr, network);
                    coins.Add(coin);
                }

                // Sign transaction and return.
                string signedTx = SoftHSMService.SignTransfer(unsignedtx.txHash, unsignedtx.keyPath, coins);

                return Ok(signedTx);
            }
            catch (JsonException ex)
            {
                return BadRequest($"Invalid JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("key/{keyPath}")]
        public IActionResult GetBTCAddress(string keyPath)
        {
            try
            {
                if (keyPath == null)
                {
                    return BadRequest("keyPath should be provided !");
                }
                
                // Convert "/" and "'" back.
                keyPath = Uri.UnescapeDataString(keyPath);

                return Ok(SoftHSMService.GetPubKey(keyPath).GetAddress(ScriptPubKeyType.Legacy, Network.TestNet).ToString());
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
