using ExcelDoc.Server.Options;

namespace ExcelDoc.Server.Tests;

public sealed class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    [Fact]
    public void Validate_RejectsEmptySecret()
    {
        var options = CreateValidOptions();
        options.SecretKey = string.Empty;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("SecretKey", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsShortSecret()
    {
        var options = CreateValidOptions();
        options.SecretKey = "short-secret";

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("at least 32", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_RejectsRepositoryPlaceholder()
    {
        var options = CreateValidOptions();
        options.SecretKey = "ExcelDoc.Jwt.Secret.Key.Change.Me.2026";

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            failure => failure.Contains("placeholder", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_AcceptsStrongSecret()
    {
        var options = CreateValidOptions();

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    private static JwtOptions CreateValidOptions() =>
        new()
        {
            Issuer = "ExcelDoc.Api",
            Audience = "ExcelDoc.Client",
            SecretKey = "0s8R7v6X5z4Q3p2N1m9K8j7H6g5F4d3S2a1W0e9T8y7U6i5O",
            ExpirationMinutes = 120
        };
}
