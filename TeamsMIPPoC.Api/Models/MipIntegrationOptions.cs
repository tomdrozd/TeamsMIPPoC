namespace TeamsMIPPoC.Api.Models;

public sealed class MipIntegrationOptions
{
    public const string SectionName = "MipIntegration";

    public string ServiceBaseUrl { get; init; } = string.Empty;
    public string LabelLookupPath { get; init; } = "/labels/lookup";
    public string Scope { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
}
