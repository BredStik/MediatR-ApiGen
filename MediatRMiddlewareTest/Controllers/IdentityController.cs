using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace MediatRMiddlewareTest.Controllers
{
    [Microsoft.AspNetCore.Mvc.Route("api/identity")]
    public class IdentityController: Controller
    {
        [AllowAnonymous]
        [HttpPost]
        public IActionResult RequestToken([FromBody]TokenRequest request)
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

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token)
                });
            }

            return BadRequest("Could not verify username and password");
        }

        [Authorize]
        [HttpPost]
        [Microsoft.AspNetCore.Mvc.Route("refresh")]
        public IActionResult RefreshToken([FromHeader(Name = "Authorization")]string jwtToken)
        {
            
            var jwtTokenCopy = jwtToken;

            var loggedInUser = HttpContext.User;

            var claims = new[]
                {
                    new Claim(ClaimTypes.Name, loggedInUser.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value)
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my_super_power_is_awesome"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "yourdomain.com",
                audience: "yourdomain.com",
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
    }

    public class TokenRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}

