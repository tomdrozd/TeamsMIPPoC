namespace TeamsMIPPoC.Api.Models;

public sealed record LabelLookupResponse(
    string FileUrl,
    string SensitivityLabel,
    string Source,
    string Evidence);
