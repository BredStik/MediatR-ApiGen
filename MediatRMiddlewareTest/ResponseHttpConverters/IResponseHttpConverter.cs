using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest.ResponseHttpConverters
{
    public interface IResponseHttpConverter<TResponse>
    {
        Task Convert(TResponse response, HttpResponse httpResponse);
    }
}
