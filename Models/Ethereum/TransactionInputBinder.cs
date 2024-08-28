using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Nethereum.RPC.Eth.DTOs;

namespace SoftHSMSigner.Models.Ethereum
{
    public class TransactionModelBinder : IModelBinder
    {
        public async Task BindModelAsync(ModelBindingContext bindingContext)
        {
            ArgumentNullException.ThrowIfNull(bindingContext);

            using var reader = new StreamReader(bindingContext.HttpContext.Request.Body);
            
            var body = await reader.ReadToEndAsync().ConfigureAwait(continueOnCapturedContext: false);
            var value = JsonConvert.DeserializeObject<TransactionInput>(body);

            bindingContext.Result = ModelBindingResult.Success(value);
        }
    }

}
