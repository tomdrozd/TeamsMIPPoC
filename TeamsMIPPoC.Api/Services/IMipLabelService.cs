using TeamsMIPPoC.Api.Models;

namespace TeamsMIPPoC.Api.Services;

public interface IMipLabelService
{
    LabelLookupResponse? GetSensitivityLabel(Uri fileUri);
}
