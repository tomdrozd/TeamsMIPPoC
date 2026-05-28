using System.Net;
using System.Net.Http.Json;
using TeamsMIPPoC.Api.Models;
using TeamsMIPPoC.Api.Services;

namespace TeamsMIPPoC.Api.Tests;

public class MipLabelServiceTests
{
    private static readonly MipIntegrationOptions Options = new()
    {
        ServiceBaseUrl = "https://mip.example",
        LabelLookupPath = "/labels/lookup",
        Scope = "api://mip/.default",
        TenantId = "tenant",
        ClientId = "client",
        ClientSecret = "secret"
    };

    [Fact]
    public async Task GetSensitivityLabelAsync_ReturnsSupportedResult_FromMipService()
    {
        var service = new MipLabelService(
            new HttpClient(new StubHttpMessageHandler(_ =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        sensitivityLabel = "Confidential",
                        source = "MIP SDK",
                        evidence = "Policy resolved for file."
                    })
                };
                return response;
            })),
            new StaticAccessTokenProvider("token"),
            Options);

        var result = await service.GetSensitivityLabelAsync(
            new Uri("https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Confidential", result.Response!.SensitivityLabel);
        Assert.Equal("MIP SDK", result.Response.Source);
    }

    [Theory]
    [InlineData("https://contoso.com/doc.docx")]
    [InlineData("http://contoso.sharepoint.com/sites/legal/doc.docx")]
    public async Task GetSensitivityLabelAsync_ReturnsUnsupportedUrl_ForInvalidInput(string url)
    {
        var service = new MipLabelService(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            new StaticAccessTokenProvider("token"),
            Options);

        var result = await service.GetSensitivityLabelAsync(new Uri(url));

        Assert.False(result.IsSuccess);
        Assert.Equal(LabelLookupFailureReason.UnsupportedUrl, result.FailureReason);
    }

    [Fact]
    public async Task GetSensitivityLabelAsync_ReturnsAuthenticationFailure_WhenUpstreamUnauthorized()
    {
        var service = new MipLabelService(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized))),
            new StaticAccessTokenProvider("token"),
            Options);

        var result = await service.GetSensitivityLabelAsync(
            new Uri("https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx"));

        Assert.False(result.IsSuccess);
        Assert.Equal(LabelLookupFailureReason.AuthenticationFailure, result.FailureReason);
    }

    [Fact]
    public async Task GetSensitivityLabelAsync_ReturnsPolicyUnavailable_WhenUpstreamFails()
    {
        var service = new MipLabelService(
            new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))),
            new StaticAccessTokenProvider("token"),
            Options);

        var result = await service.GetSensitivityLabelAsync(
            new Uri("https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx"));

        Assert.False(result.IsSuccess);
        Assert.Equal(LabelLookupFailureReason.PolicyUnavailable, result.FailureReason);
    }

    private sealed class StaticAccessTokenProvider(string token) : IAccessTokenProvider
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult(token);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
