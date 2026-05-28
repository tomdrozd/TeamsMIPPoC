using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TeamsMIPPoC.Api.Models;
using TeamsMIPPoC.Api.Services;

namespace TeamsMIPPoC.Api.Tests;

public class SensitivityLabelEndpointTests : IClassFixture<SensitivityLabelEndpointTests.TestAppFactory>
{
    private readonly HttpClient _client;

    public SensitivityLabelEndpointTests(TestAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_ForNonAbsoluteUrl()
    {
        var response = await _client.PostAsJsonAsync("/api/sensitivity-label", new { fileUrl = "not-a-url" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The fileUrl must be a valid absolute URL.", await GetErrorMessageAsync(response));
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_ForHttpSharePointUrl()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/sensitivity-label",
            new { fileUrl = "http://contoso.sharepoint.com/sites/legal/doc.docx" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("The fileUrl must use HTTPS.", await GetErrorMessageAsync(response));
    }

    [Fact]
    public async Task Post_ReturnsBadRequest_ForUnsupportedHost()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/sensitivity-label",
            new { fileUrl = "https://example.com/files/doc.docx" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "Only SharePoint Online and OneDrive for Business URLs are supported.",
            await GetErrorMessageAsync(response));
    }

    [Fact]
    public async Task Post_ReturnsExpectedPayload_ForSupportedUrl()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/sensitivity-label",
            new { fileUrl = "https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LabelLookupResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Confidential", payload.SensitivityLabel);
        Assert.Equal("MIP SDK", payload.Source);
    }

    [Fact]
    public async Task Messages_ReturnsReply_ForSupportedUrl()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/messages",
            new { text = "please check https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TeamsMessageResponse>();

        Assert.NotNull(payload);
        Assert.Contains("Sensitivity label: Confidential", payload.Reply);
    }

    [Fact]
    public async Task Messages_ParsesUrl_WithTrailingPunctuation()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/messages",
            new { text = "check this: https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx." });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TeamsMessageResponse>();

        Assert.NotNull(payload);
        Assert.Contains("Sensitivity label: Confidential", payload.Reply);
    }

    private static async Task<string?> GetErrorMessageAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return payload?.GetValueOrDefault("error");
    }

    public sealed class TestAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMipLabelService>();
                services.AddSingleton<IMipLabelService, FakeMipLabelService>();
            });
        }
    }

    private sealed class FakeMipLabelService : IMipLabelService
    {
        public Task<LabelLookupResult> GetSensitivityLabelAsync(Uri fileUri, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(fileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !fileUri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(LabelLookupResult.Failure(
                    LabelLookupFailureReason.UnsupportedUrl,
                    "Only SharePoint Online and OneDrive for Business URLs are supported."));
            }

            var response = new LabelLookupResponse(
                fileUri.ToString(),
                "Confidential",
                "MIP SDK",
                "Resolved by fake MIP service for integration tests.");
            return Task.FromResult(LabelLookupResult.Success(response));
        }
    }
}
