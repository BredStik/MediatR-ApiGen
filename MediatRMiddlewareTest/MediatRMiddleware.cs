using MediatR;
using MediatRMiddlewareTest.Commands;
using MediatRMiddlewareTest.ResponseHttpConverters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema.Generation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest
{
    public class RequestRoute
    {
        public RouteAttribute Route { get; set; }
        public Type RequestType { get; set; }
    }

    public class MediatRMiddleware
    {
        private readonly IMediator _mediator;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private static IDictionary<string, Func<HttpContext, IMediator, RouteValueDictionary, Task>> requestRoutes = Assembly.GetAssembly(typeof(MediatRMiddleware)).GetTypes().Where(t => t.GetCustomAttribute<RouteAttribute>() != null).ToDictionary(
            t => t.GetCustomAttribute<RouteAttribute>().Route,
            t => new Func<HttpContext, IMediator, RouteValueDictionary, Task>(async (context, mediator, values) => {
                var routeAttribute = t.GetCustomAttribute<RouteAttribute>();
                //check matching http method
                if (routeAttribute.HttpMethod.Equals(context.Request.Method, StringComparison.InvariantCultureIgnoreCase))
                {
                    dynamic mediatrRequest = JsonConvert.DeserializeObject(await new StreamReader(context.Request.Body).ReadToEndAsync().ConfigureAwait(false), t);

                    if(mediatrRequest == null)
                    {
                        mediatrRequest = Activator.CreateInstance(t);
                    }

                    if(values.Any())
                    {
                        //see if matching property
                        foreach(var value in values)
                        {
                            var prop = t.GetProperty(value.Key);
                            
                            prop?.SetValue(mediatrRequest, JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value.Value), prop.PropertyType));
                        }
                    }

                    var response = await mediator.Send(mediatrRequest).ConfigureAwait(false);
                    string jsonResponse = JsonConvert.SerializeObject(response);

                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsync(jsonResponse).ConfigureAwait(false);
                }
            })
        );

        public MediatRMiddleware(RequestDelegate next, IRouteBuilder routeBuilder, ILoggerFactory loggerFactory, IMediator mediator)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<MediatRMiddleware>();
            _mediator = mediator;
        }

        public async Task Invoke(HttpContext context)
        {
            RouteValueDictionary values = null;
            var matchingRoute = requestRoutes.Keys.FirstOrDefault(route => RouteMatcher.IsMatch(route, context.Request.Path.ToString(), out values));

            if(matchingRoute == null)
            {
                await _next.Invoke(context);
                return;
            }

            try
            {
                await requestRoutes[matchingRoute](context, _mediator, values).ConfigureAwait(false);
            }
            catch(ApplicationException exc)
            {
                var exceptionHandlingDictionary = new Dictionary<Type, Action<HttpContext>>
                {
                    { typeof(UnauthorizedAccessException), async (ctx) => { ctx.Response.StatusCode = 401; await ctx.Response.WriteAsync("Invalid or missing credentials").ConfigureAwait(false); } },
                    { typeof(ApplicationException), async (ctx) => { ctx.Response.StatusCode = 500; await ctx.Response.WriteAsync("Unknown server error").ConfigureAwait(false); } }
                };


                if(exceptionHandlingDictionary.ContainsKey(exc.GetType()))
                {
                    exceptionHandlingDictionary[exc.GetType()](context);
                }
                else
                {
                    exceptionHandlingDictionary[typeof(ApplicationException)](context);
                }

                return;
            }

            if(context.Response.HasStarted)
            {
                return;
            }

            await _next.Invoke(context);
        }
    }

    public static class MediatRMiddlewareExtensions
    {
        public static IApplicationBuilder UseMediatRMiddleware(this IApplicationBuilder builder, IRouteBuilder routeBuilder)
        {
            return builder.UseMiddleware<MediatRMiddleware>(routeBuilder);
        }

        public static IApplicationBuilder ConfigureMediatRRoutes(this IApplicationBuilder builder, IRouteBuilder routeBuilder)
        {
            var routeAttributes = Assembly.GetAssembly(typeof(MediatRMiddleware)).GetTypes().Where(t => t.GetCustomAttribute<RouteAttribute>() != null).Select(t => new { Type = t, RouteAttribute = t.GetCustomAttribute<RouteAttribute>() });

            foreach (var routeAttribute in routeAttributes)
            {
                routeBuilder.MapVerb(routeAttribute.RouteAttribute.HttpMethod, routeAttribute.RouteAttribute.Route, async (request, response, routeData) => {
                    var mediator = request.HttpContext.RequestServices.GetService(typeof(IMediator)) as IMediator;

                    dynamic mediatrRequest = JsonConvert.DeserializeObject(await new StreamReader(request.Body).ReadToEndAsync().ConfigureAwait(false), routeAttribute.Type);

                    if (mediatrRequest == null)
                    {
                        mediatrRequest = Activator.CreateInstance(routeAttribute.Type);
                    }
                    
                    if (request.Query.Any())
                    {
                        foreach (var queryString in request.Query)
                        {
                            var prop = routeAttribute.Type.GetProperties().FirstOrDefault(x => x.Name.Equals(queryString.Key, StringComparison.InvariantCultureIgnoreCase));

                            prop?.SetValue(mediatrRequest, JsonConvert.DeserializeObject(JsonConvert.SerializeObject(queryString.Value.FirstOrDefault()), prop.PropertyType));
                        }
                    }

                    if (routeData.Values.Any())
                    {
                        //see if matching property
                        foreach (var value in routeData.Values)
                        {
                            var prop = routeAttribute.Type.GetProperties().FirstOrDefault(x => x.Name.Equals(value.Key, StringComparison.InvariantCultureIgnoreCase));

                            prop?.SetValue(mediatrRequest, JsonConvert.DeserializeObject(JsonConvert.SerializeObject(value.Value), prop.PropertyType));
                        }
                    }                                        

                    var mediatorResponse = await mediator.Send(mediatrRequest).ConfigureAwait(false);

                    //var converters = request.HttpContext.RequestServices.GetRequiredServices(typeof(IResponseHttpConverter<>));
                    dynamic converter = request.HttpContext.RequestServices.GetService(typeof(IResponseHttpConverter<>).MakeGenericType(mediatorResponse.GetType()));

                    await converter.Convert(mediatorResponse, response);

                    //string jsonResponse = JsonConvert.SerializeObject(mediatorResponse);
                    //
                    //response.ContentType = "application/json";
                    //
                    //await response.WriteAsync(jsonResponse).ConfigureAwait(false);
                });
            }

            return builder;
        }
    }

    public static class RouteMatcher
    {
        public static bool IsMatch(string routeTemplate, string requestPath, out RouteValueDictionary routeValues)
        {
            var template = TemplateParser.Parse(routeTemplate);

            var matcher = new TemplateMatcher(template, GetDefaults(template));

            routeValues = new RouteValueDictionary();

            return matcher.TryMatch(requestPath, routeValues);
        }

        // This method extracts the default argument values from the template.
        private static RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
        {
            var result = new RouteValueDictionary();

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    result.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            return result;
        }
    }
}
