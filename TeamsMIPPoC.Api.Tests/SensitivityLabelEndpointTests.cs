using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TeamsMIPPoC.Api.Models;

namespace TeamsMIPPoC.Api.Tests;

public class SensitivityLabelEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SensitivityLabelEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Post_ReturnsExpectedPayload_ForSupportedUrl()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/sensitivity-label",
            new { fileUrl = "https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx" });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LabelLookupResponse>();

        Assert.NotNull(payload);
        Assert.Equal("Confidential", payload.SensitivityLabel);
        Assert.Equal("MIP SDK PoC", payload.Source);
    }

    private static async Task<string?> GetErrorMessageAsync(HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        return payload?.GetValueOrDefault("error");
    }
}
