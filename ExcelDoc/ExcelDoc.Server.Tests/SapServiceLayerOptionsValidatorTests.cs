using ExcelDoc.Server.Options;

namespace ExcelDoc.Server.Tests;

public sealed class SapServiceLayerOptionsValidatorTests
{
    private readonly SapServiceLayerOptionsValidator _validator = new();

    [Fact]
    public void Validate_AcceptsValidAllowList()
    {
        var options = CreateValidOptions();

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_RejectsEmptyAllowList()
    {
        var options = CreateValidOptions();
        options.Bases = [];

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("at least one SAP database"));
    }

    [Fact]
    public void Validate_RejectsNullAllowListEntry()
    {
        var options = CreateValidOptions();
        options.Bases.Add(null!);

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("must be an object"));
    }

    [Fact]
    public void Validate_RejectsDuplicateDatabaseIgnoringCase()
    {
        var options = CreateValidOptions();
        options.Bases.Add(new SapBaseOptions
        {
            Database = "sboprod",
            Description = "Duplicate"
        });

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("duplicated"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("sap-server:50000")]
    [InlineData("ftp://sap-server/b1s/v1")]
    public void Validate_RejectsInvalidBaseUrl(string baseUrl)
    {
        var options = CreateValidOptions();
        options.BaseUrl = baseUrl;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("BaseUrl"));
    }

    [Fact]
    public void Validate_RejectsHttpBaseUrl()
    {
        var options = CreateValidOptions();
        options.BaseUrl = "http://sap.example.test:50000/b1s/v1";

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("HTTPS", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsDatabaseWithOuterWhitespace()
    {
        var options = CreateValidOptions();
        options.Bases[0].Database = " SBOPROD ";

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("must not start or end with whitespace"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(601)]
    public void Validate_RejectsUnsafeTimeout(int timeoutSeconds)
    {
        var options = CreateValidOptions();
        options.RequestTimeoutSeconds = timeoutSeconds;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("RequestTimeoutSeconds"));
    }

    private static SapServiceLayerOptions CreateValidOptions() =>
        new()
        {
            BaseUrl = "https://sap.example.test:50000/b1s/v1/",
            RequestTimeoutSeconds = 100,
            Bases =
            [
                new SapBaseOptions
                {
                    Database = "SBOPROD",
                    Description = "Production"
                }
            ]
        };
}
