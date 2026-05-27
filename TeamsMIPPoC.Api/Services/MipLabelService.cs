using TeamsMIPPoC.Api.Models;

namespace TeamsMIPPoC.Api.Services;

public sealed class MipLabelService : IMipLabelService
{
    public LabelLookupResponse? GetSensitivityLabel(Uri fileUri)
    {
        if (!IsSupportedBusinessUrl(fileUri))
        {
            return null;
        }

        var extension = Path.GetExtension(fileUri.AbsolutePath).ToLowerInvariant();
        var sensitivityLabel = extension switch
        {
            ".docx" or ".xlsx" or ".pptx" => "Confidential",
            ".pdf" => "General",
            _ => "Public"
        };

        return new LabelLookupResponse(
            fileUri.ToString(),
            sensitivityLabel,
            "MIP SDK PoC",
            "Simulated lookup for Teams PoC. Replace with a real MIP SDK policy/label read.");
    }

    private static bool IsSupportedBusinessUrl(Uri fileUri)
    {
        if (!string.Equals(fileUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var host = fileUri.Host;
        if (!host.Contains("sharepoint.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith("-my.sharepoint.com", StringComparison.OrdinalIgnoreCase);
    }
}
