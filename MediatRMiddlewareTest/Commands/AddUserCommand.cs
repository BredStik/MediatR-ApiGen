using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MediatRMiddlewareTest.Commands
{
    //[Authorize()]
    [Route("api/commands/addUser/{Id}", "POST")]
    public class AddUserCommand: IRequest<string>
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Guid Id { get; set; }
    }

    public class AddUserCommandHandler : IRequestHandler<AddUserCommand, string>
    {
        public async Task<string> Handle(AddUserCommand request, CancellationToken cancellationToken)
        {
            return $"hello {request.Name}!";
        }
    }
}
