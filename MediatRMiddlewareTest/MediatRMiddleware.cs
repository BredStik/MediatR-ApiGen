using MediatR;
using MediatRMiddlewareTest.Commands;
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

        public MediatRMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IMediator mediator)
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
        public static IApplicationBuilder UseMediatRMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MediatRMiddleware>();
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
