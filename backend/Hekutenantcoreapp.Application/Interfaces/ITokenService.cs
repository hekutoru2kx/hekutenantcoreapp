using Hekutenantcoreapp.Domain.Models;

namespace Hekutenantcoreapp.Application.Interfaces;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(GenerateTokenRequest request);
}