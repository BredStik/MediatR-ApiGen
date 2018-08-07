using System;
using System.Net.Http;

namespace MediatRMiddlewareTest
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RouteAttribute: Attribute
    {
        public string Route { get; set; }
        public string HttpMethod { get; set; }
        public Type RequestType { get; set; }

        public RouteAttribute(string route, string httpMethod)
        {
            Route = route;
            HttpMethod = httpMethod;
        }
    }
}
