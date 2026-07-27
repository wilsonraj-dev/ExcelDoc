using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Options;

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MinimumSecretLength = 32;

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add($"{JwtOptions.SectionName}:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add($"{JwtOptions.SectionName}:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey) ||
            options.SecretKey.Length < MinimumSecretLength)
        {
            failures.Add(
                $"{JwtOptions.SectionName}:SecretKey must contain at least {MinimumSecretLength} characters.");
        }

        if (!string.IsNullOrEmpty(options.SecretKey) &&
            options.SecretKey.Contains("Change.Me", StringComparison.OrdinalIgnoreCase))
        {
            failures.Add(
                $"{JwtOptions.SectionName}:SecretKey must not use the repository placeholder.");
        }

        if (options.ExpirationMinutes is < 1 or > 1440)
        {
            failures.Add(
                $"{JwtOptions.SectionName}:ExpirationMinutes must be between 1 and 1440.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
