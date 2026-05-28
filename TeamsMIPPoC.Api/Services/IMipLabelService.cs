using TeamsMIPPoC.Api.Models;

namespace TeamsMIPPoC.Api.Services;

public interface IMipLabelService
{
    Task<LabelLookupResult> GetSensitivityLabelAsync(Uri fileUri, CancellationToken cancellationToken = default);
}
