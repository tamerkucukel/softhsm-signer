using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.JsonConverters;
using Nethereum.Web3.Accounts;
using SoftHSM_API_NET_8.Models;
using SoftHSM_API_NET_8.Services;
using Newtonsoft.Json;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Hex.HexTypes;
using System.Numerics;

namespace SoftHSM_API_NET_8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SoftHSMController : ControllerBase
    {
        [HttpPost("transaction/sign/btc")]
        public IActionResult SignBTCTransaction(UnsignedBTCTransaction unsignedtx)
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
                string signedTx = SoftHSMService.SignBTCTransfer(unsignedtx.txHash, unsignedtx.keyPath, coins);

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

        [HttpPost("transaction/sign/eth")]
        public IActionResult SignETHTransaction(UnsignedETHTransaction unsignedTx)
        {
            try
            {   
                // Create transaction input from payload.
                var txInput = new TransactionInput
                {
                    Data = unsignedTx.Data,
                    From = unsignedTx.From,
                    To = unsignedTx.To,
                    Value = new HexBigInteger(BigInteger.Parse(unsignedTx.Value)),
                    Gas = new HexBigInteger(BigInteger.Parse(unsignedTx.Gas)),
                    GasPrice = new HexBigInteger(BigInteger.Parse(unsignedTx.GasPrice)),
                    Nonce = new HexBigInteger(BigInteger.Parse(unsignedTx.Nonce)),
                    ChainId = new HexBigInteger(BigInteger.Parse(unsignedTx.ChainId))
                };
                var account = new Account(SoftHSMService.GetExtKey(unsignedTx.KeyPath).PrivateKey.ToHex()); // 64-character hexadecimal string.
                var signedTx = new AccountOfflineTransactionSigner().SignTransaction(account, txInput, txInput.ChainId.Value); // Sign with key from HSM.
                return Ok(signedTx); //Return raw transaction.
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

        [HttpGet("address/btc/{keyPath}")]
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

                return Ok(SoftHSMService.GetExtKey(keyPath).GetPublicKey().GetAddress(ScriptPubKeyType.Legacy, Network.TestNet).ToString());
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("address/eth/{keyPath}")]
        public IActionResult GetETHAddress(string keyPath)
        {
            try
            {
                if (keyPath == null)
                {
                    return BadRequest("keyPath should be provided !");
                }
                // Convert "/" and "'" back.
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
