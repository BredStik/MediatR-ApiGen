using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest.Commands
{
    [Authorize()]
    [Route("api/commands/refreshToken", "POST")]
    public class RefreshTokenCommand: IRequest<object>
    {
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, object>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RefreshTokenCommandHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<object> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var username = _httpContextAccessor.HttpContext.User.Identity.Name;

            var claims = new[]
                {
                    new Claim(ClaimTypes.Name, username)
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
    }
}
