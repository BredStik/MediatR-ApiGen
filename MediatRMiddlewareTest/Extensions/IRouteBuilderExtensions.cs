using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest.Extensions
{
    public static class IRouteBuilderExtensions
    {
        public static void MagGet<TRequest>(this IRouteBuilder routeBuilder, string template) where TRequest: IBaseRequest
        {
            routeBuilder.MapGet(template, BuildMediatorHttpHandlerAsync<TRequest>);
        }

        public static void MagPost<TRequest>(this IRouteBuilder routeBuilder, string template) where TRequest : IBaseRequest
        {
            routeBuilder.MapPost(template, BuildMediatorHttpHandlerAsync<TRequest>);
        }

        private static async Task BuildMediatorHttpHandlerAsync<TRequest>(HttpRequest request, HttpResponse response, RouteData routeData) where TRequest: IBaseRequest
        {
            var requestType = typeof(TRequest);

            var mediator = request.HttpContext.RequestServices.GetService(typeof(IMediator)) as IMediator;

            dynamic mediatrRequest = JsonConvert.DeserializeObject(await new StreamReader(request.Body).ReadToEndAsync().ConfigureAwait(false), requestType);

            if (mediatrRequest == null)
            {
                mediatrRequest = Activator.CreateInstance(requestType);
            }

            if (request.Query.Any())
            {
                foreach (var queryString in request.Query)
                {
                    var prop = requestType.GetProperties().FirstOrDefault(x => x.Name.Equals(queryString.Key, StringComparison.InvariantCultureIgnoreCase));

                    prop?.SetValue(mediatrRequest, JsonConvert.DeserializeObject(JsonConvert.SerializeObject(queryString.Value.FirstOrDefault()), prop.PropertyType));
                }
            }

            if (routeData.Values.Any())
            {
                //see if matching property
                foreach (var value in routeData.Values)
                {
                    var prop = requestType.GetProperties().FirstOrDefault(x => x.Name.Equals(value.Key, StringComparison.InvariantCultureIgnoreCase));

                    prop?.SetValue(mediatrRequest, JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value.Value), prop.PropertyType));
                }
            }


            var mediatorResponse = await mediator.Send(mediatrRequest).ConfigureAwait(false);
            string jsonResponse = JsonConvert.SerializeObject(mediatorResponse);

            response.ContentType = "application/json";

            await response.WriteAsync(jsonResponse).ConfigureAwait(false);
        }
    }
}
