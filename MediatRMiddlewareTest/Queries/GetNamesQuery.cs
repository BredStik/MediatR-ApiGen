using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest.Queries
{
    [Route("api/queries/getNames", "GET")]
    public class GetNamesQuery: IRequest<IEnumerable<string>>
    {
        public int MaxItems { get; set; }
    }

    public class GetNamesQueryHandler : IRequestHandler<GetNamesQuery, IEnumerable<string>>
    {
        public async Task<IEnumerable<string>> Handle(GetNamesQuery request, CancellationToken cancellationToken)
        {
            return new[] { "Math", "John", "Marc", "Peter" }.Take(request.MaxItems);
        }
    }
}
