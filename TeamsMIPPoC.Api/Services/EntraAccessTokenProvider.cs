using Azure.Core;
using Azure.Identity;
using TeamsMIPPoC.Api.Models;

namespace TeamsMIPPoC.Api.Services;

public sealed class EntraAccessTokenProvider : IAccessTokenProvider
{
    private readonly TokenCredential _credential;
    private readonly string[] _scopes;

    public EntraAccessTokenProvider(MipIntegrationOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TenantId) ||
            string.IsNullOrWhiteSpace(options.ClientId) ||
            string.IsNullOrWhiteSpace(options.ClientSecret) ||
            string.IsNullOrWhiteSpace(options.Scope))
        {
            throw new InvalidOperationException(
                "MipIntegration requires TenantId, ClientId, ClientSecret, and Scope.");
        }

        _credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
        _scopes = [options.Scope];
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = await _credential.GetTokenAsync(new TokenRequestContext(_scopes), cancellationToken);
        return token.Token;
    }
}
