using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookManager.Application.Abstractions.Security;
using BookManager.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace BookManager.Infrastructure.Security
{
    public sealed class JwtTokenGenerator :
        IJwtTokenGenerator
    {
        private readonly JwtOptions _options;

        public JwtTokenGenerator(JwtOptions options)
        {
            _options = options;
        }

        public string Generate(User user)
        {
            var now = DateTime.UtcNow;

            var claims = new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Login),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
            };

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _options.Secret));

            var credentials =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer: _options.Issuer,
                    audience: _options.Audience,
                    claims: claims,
                    notBefore: now,
                    expires:
                        now.AddMinutes(
                            _options.LifetimeMinutes),
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
