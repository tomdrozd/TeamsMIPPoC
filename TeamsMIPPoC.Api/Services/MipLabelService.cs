using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Azure.Identity;
using TeamsMIPPoC.Api.Models;

namespace TeamsMIPPoC.Api.Services;

public sealed class MipLabelService : IMipLabelService
{
    private readonly HttpClient _httpClient;
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly MipIntegrationOptions _options;

    public MipLabelService(
        HttpClient httpClient,
        IAccessTokenProvider accessTokenProvider,
        MipIntegrationOptions options)
    {
        _httpClient = httpClient;
        _accessTokenProvider = accessTokenProvider;
        _options = options;
    }

    public async Task<LabelLookupResult> GetSensitivityLabelAsync(Uri fileUri, CancellationToken cancellationToken = default)
    {
        if (!IsSupportedBusinessUrl(fileUri))
        {
            return LabelLookupResult.Failure(
                LabelLookupFailureReason.UnsupportedUrl,
                "Only SharePoint Online and OneDrive for Business URLs are supported.");
        }

        if (!Uri.TryCreate(_options.ServiceBaseUrl, UriKind.Absolute, out var baseUri))
        {
            return LabelLookupResult.Failure(
                LabelLookupFailureReason.PolicyUnavailable,
                "MIP integration is not configured. Set MipIntegration:ServiceBaseUrl.");
        }

        try
        {
            var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, _options.LabelLookupPath))
            {
                Content = JsonContent.Create(new { fileUrl = fileUri.ToString() })
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return LabelLookupResult.Failure(
                    LabelLookupFailureReason.AuthenticationFailure,
                    "Authentication against the MIP service failed.");
            }

            if ((int)response.StatusCode >= 500)
            {
                return LabelLookupResult.Failure(
                    LabelLookupFailureReason.PolicyUnavailable,
                    "The MIP service is unavailable.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return LabelLookupResult.Failure(
                    LabelLookupFailureReason.PolicyUnavailable,
                    $"MIP service returned {(int)response.StatusCode}.");
            }

            var payload = await response.Content.ReadFromJsonAsync<MipLookupPayload>(cancellationToken);
            if (payload is null || string.IsNullOrWhiteSpace(payload.SensitivityLabel))
            {
                return LabelLookupResult.Failure(
                    LabelLookupFailureReason.PolicyUnavailable,
                    "MIP service returned an invalid payload.");
            }

            var resolved = new LabelLookupResponse(
                fileUri.ToString(),
                payload.SensitivityLabel,
                payload.Source ?? "MIP SDK",
                payload.Evidence ?? "Label resolved from MIP SDK-backed service.");

            return LabelLookupResult.Success(resolved);
        }
        catch (AuthenticationFailedException)
        {
            return LabelLookupResult.Failure(
                LabelLookupFailureReason.AuthenticationFailure,
                "Entra ID token acquisition failed for MIP service.");
        }
        catch (HttpRequestException)
        {
            return LabelLookupResult.Failure(
                LabelLookupFailureReason.PolicyUnavailable,
                "Unable to reach the MIP service.");
        }
    }

    private static bool IsSupportedBusinessUrl(Uri fileUri)
    {
        if (!fileUri.IsAbsoluteUri)
        {
            return false;
        }

        if (!string.Equals(fileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = fileUri.Host;
        return host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record MipLookupPayload(string SensitivityLabel, string? Source, string? Evidence);
}
