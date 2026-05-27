using TeamsMIPPoC.Api.Services;

namespace TeamsMIPPoC.Api.Tests;

public class MipLabelServiceTests
{
    private readonly MipLabelService _service = new();

    [Theory]
    [InlineData("https://contoso.sharepoint.com/sites/legal/Shared%20Documents/contract.docx", "Confidential")]
    [InlineData("https://contoso-my.sharepoint.com/personal/user_contoso_com/Documents/report.pdf", "General")]
    [InlineData("https://contoso.sharepoint.com/sites/hr/Shared%20Documents/readme.txt", "Public")]
    public void GetSensitivityLabel_ReturnsExpectedLabel_ForSupportedLinks(string url, string expectedLabel)
    {
        var result = _service.GetSensitivityLabel(new Uri(url));

        Assert.NotNull(result);
        Assert.Equal(expectedLabel, result.SensitivityLabel);
        Assert.Equal("MIP SDK PoC", result.Source);
    }

    [Theory]
    [InlineData("https://contoso.com/doc.docx")]
    [InlineData("http://contoso.sharepoint.com/sites/legal/doc.docx")]
    public void GetSensitivityLabel_ReturnsNull_ForUnsupportedLinks(string url)
    {
        var result = _service.GetSensitivityLabel(new Uri(url));

        Assert.Null(result);
    }
}
