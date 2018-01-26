using MediatR;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest.Commands
{
    [Route("api/commands/requestToken", "POST")]
    public class RequestTokenCommand: IRequest<object>
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class RequestTokenCommandHandler : IRequestHandler<RequestTokenCommand, object>
    {
        public async Task<object> Handle(RequestTokenCommand request, CancellationToken cancellationToken)
        {
            if (request.Username == "Jon" && request.Password == "Again, not for production use, DEMO ONLY!")
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, request.Username)
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my_super_power_is_awesome"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "yourdomain.com",
                    audience: "yourdomain.com",
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds);

                return new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token)
                };
            }

            throw new InvalidOperationException("Invalid credentials");
        }
    }
}
