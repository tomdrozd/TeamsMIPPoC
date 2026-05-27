using TeamsMIPPoC.Api.Models;
using TeamsMIPPoC.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IMipLabelService, MipLabelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/sensitivity-label", (LabelLookupRequest request, IMipLabelService mipLabelService) =>
{
    if (!Uri.TryCreate(request.FileUrl, UriKind.Absolute, out var fileUri))
    {
        return Results.BadRequest(new { error = "The fileUrl must be a valid absolute URL." });
    }

    var lookup = mipLabelService.GetSensitivityLabel(fileUri);
    return lookup is null
        ? Results.BadRequest(new { error = "Only SharePoint Online and OneDrive for Business URLs are supported." })
        : Results.Ok(lookup);
})
.WithName("GetSensitivityLabel");

app.Run();
