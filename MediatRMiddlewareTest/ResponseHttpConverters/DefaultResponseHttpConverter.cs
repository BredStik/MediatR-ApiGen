using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace MediatRMiddlewareTest.ResponseHttpConverters
{
    public class DefaultResponseHttpConverter<TResponse> : IResponseHttpConverter<TResponse>
    {
        public virtual async Task Convert(TResponse response, HttpResponse httpResponse)
        {
            string jsonResponse = JsonConvert.SerializeObject(response);

            httpResponse.ContentType = "application/json";
            
            await httpResponse.WriteAsync(jsonResponse).ConfigureAwait(false);
        }
    }
}
