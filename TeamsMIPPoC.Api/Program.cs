using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using TeamsMIPPoC.Api.Models;
using TeamsMIPPoC.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
var mipOptions = builder.Configuration.GetSection(MipIntegrationOptions.SectionName).Get<MipIntegrationOptions>()
    ?? new MipIntegrationOptions();
builder.Services.AddSingleton(mipOptions);
builder.Services.AddSingleton<IAccessTokenProvider, EntraAccessTokenProvider>();
builder.Services.AddHttpClient<IMipLabelService, MipLabelService>();

var authOptions = builder.Configuration.GetSection(ApiAuthenticationOptions.SectionName).Get<ApiAuthenticationOptions>()
    ?? new ApiAuthenticationOptions();
if (authOptions.Enabled)
{
    if (string.IsNullOrWhiteSpace(authOptions.Authority) || string.IsNullOrWhiteSpace(authOptions.Audience))
    {
        throw new InvalidOperationException(
            "Authentication is enabled but Authentication:Authority or Authentication:Audience is missing.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authOptions.Authority;
            options.Audience = authOptions.Audience;
        });
    builder.Services.AddAuthorization();
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

if (authOptions.Enabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

var labelRoute = app.MapPost("/api/sensitivity-label", async (
    LabelLookupRequest request,
    IMipLabelService mipLabelService,
    CancellationToken cancellationToken) =>
{
    if (!Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var fileUri))
    {
        return Results.BadRequest(new { error = "The fileUrl must be a valid absolute URL." });
    }

    if (!string.Equals(fileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "The fileUrl must use HTTPS." });
    }

    var lookup = await mipLabelService.GetSensitivityLabelAsync(fileUri, cancellationToken);
    if (lookup.IsSuccess)
    {
        return Results.Ok(lookup.Response);
    }

    return lookup.FailureReason switch
    {
        LabelLookupFailureReason.UnsupportedUrl =>
            Results.BadRequest(new { error = lookup.FailureMessage }),
        LabelLookupFailureReason.AuthenticationFailure =>
            Results.StatusCode(StatusCodes.Status502BadGateway),
        LabelLookupFailureReason.PolicyUnavailable =>
            Results.StatusCode(StatusCodes.Status503ServiceUnavailable),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
    };
})
.WithName("GetSensitivityLabel")
.WithOpenApi();

if (authOptions.Enabled)
{
    labelRoute.RequireAuthorization();
}

var teamsMessageRoute = app.MapPost("/api/messages", async (
    TeamsMessageRequest request,
    IMipLabelService mipLabelService,
    CancellationToken cancellationToken) =>
{
    var fileUrl = ExtractFirstUrl(request.Text);
    if (fileUrl is null)
    {
        return Results.BadRequest(new TeamsMessageResponse("Please paste an HTTPS SharePoint or OneDrive file URL."));
    }

    if (!Uri.TryCreate(fileUrl, UriKind.Absolute, out var fileUri) ||
        !string.Equals(fileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new TeamsMessageResponse("Please provide a valid HTTPS URL."));
    }

    var lookup = await mipLabelService.GetSensitivityLabelAsync(fileUri, cancellationToken);
    if (!lookup.IsSuccess)
    {
        var failureReply = lookup.FailureReason switch
        {
            LabelLookupFailureReason.UnsupportedUrl =>
                "Only SharePoint Online and OneDrive for Business URLs are supported.",
            LabelLookupFailureReason.AuthenticationFailure =>
                "I cannot access MIP right now due to authentication failure.",
            LabelLookupFailureReason.PolicyUnavailable =>
                "MIP policy service is unavailable right now.",
            _ =>
                "Unexpected error while reading the sensitivity label."
        };

        return Results.Ok(new TeamsMessageResponse(failureReply));
    }

    return Results.Ok(new TeamsMessageResponse(
        $"Sensitivity label: {lookup.Response!.SensitivityLabel} ({lookup.Response.Source})"));
})
.WithName("HandleTeamsMessage")
.WithOpenApi();

if (authOptions.Enabled)
{
    teamsMessageRoute.RequireAuthorization();
}

app.Run();

static string? ExtractFirstUrl(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return null;
    }

    var match = Regex.Match(text, @"https://\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    return match.Success ? match.Value.TrimEnd('.', ',', ';', ':', ')', ']', '}') : null;
}

public partial class Program;
