namespace TeamsMIPPoC.Api.Models;

public enum LabelLookupFailureReason
{
    UnsupportedUrl,
    AuthenticationFailure,
    PolicyUnavailable
}

public sealed record LabelLookupResult(
    LabelLookupResponse? Response,
    LabelLookupFailureReason? FailureReason,
    string? FailureMessage)
{
    public bool IsSuccess => Response is not null;

    public static LabelLookupResult Success(LabelLookupResponse response) => new(response, null, null);

    public static LabelLookupResult Failure(LabelLookupFailureReason reason, string? message = null) =>
        new(null, reason, message);
}
