namespace TeamsMIPPoC.Api.Models;

public sealed class ApiAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; init; } = true;
    public string Authority { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
}
