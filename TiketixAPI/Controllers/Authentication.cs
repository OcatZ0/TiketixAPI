using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TiketixAPI.Models;
using TiketixAPI.Models.DTO;

namespace TiketixAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class Authentication : ControllerBase
    {
        private readonly TiketixContext _dB;
        public Authentication(TiketixContext dB) { _dB = dB; }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDTO loginDTO)
        {
            var user = _dB.Users.FirstOrDefaultAsync(q => q.Email == loginDTO.Email && q.Password == loginDTO.Password).Result;

            if (user == null)
            {
                return Unauthorized("Wrong username or password");
            }

            var claims = new Claim[]
            {
                new Claim("userId", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("4fd2d7301a271708d151a5696a5ee999b5617e2a226ce0b2c0d152d3eef94ce8")); // SHA256 of fadli_ocatz
            var cred = new SigningCredentials(key, "HS256");

            var token = new JwtSecurityToken(signingCredentials: cred, claims: claims, expires: DateTime.Now.AddMinutes(10));

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiredAt = DateTime.Now.AddMinutes(10),
            });
        }
    }
}
