using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MediatRMiddlewareTest.ResponseHttpConverters
{
    public class StringEnumHttpConverter : DefaultResponseHttpConverter<string[]>
    {
        public override async Task Convert(string[] response, HttpResponse httpResponse)
        {
            await base.Convert(response, httpResponse).ConfigureAwait(false);
        }
    }
}
