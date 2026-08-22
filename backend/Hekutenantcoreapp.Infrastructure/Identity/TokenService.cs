using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Application.Resources;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IStringLocalizer<Messages> _localizer;

    public TokenService(IConfiguration configuration, RoleManager<IdentityRole> roleManager, IStringLocalizer<Messages> localizer)
    {
        _configuration = configuration;
        _roleManager = roleManager;
        _localizer = localizer;
    }

    public async Task<string> GenerateTokenAsync(GenerateTokenRequest request)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new Exception(_localizer["JwtKeyNotConfigured"])));

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId),
            new Claim(JwtRegisteredClaimNames.Email, request.Email),
            new Claim("userName", request.UserName)
        };

        if (request.TenantId.HasValue)
            claims.Add(new Claim("tenant_id", request.TenantId.Value.ToString()));

        var addedPermissionClaims = new HashSet<(string Type, string Value)>();

        foreach (var roleName in request.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));

            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null) continue;

            foreach (var claim in await _roleManager.GetClaimsAsync(role))
            {
                if (addedPermissionClaims.Add((claim.Type, claim.Value)))
                    claims.Add(claim);
            }
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}