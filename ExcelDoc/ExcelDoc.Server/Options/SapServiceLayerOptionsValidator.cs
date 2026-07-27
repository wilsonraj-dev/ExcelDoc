using Microsoft.Extensions.Options;

namespace ExcelDoc.Server.Options;

public sealed class SapServiceLayerOptionsValidator : IValidateOptions<SapServiceLayerOptions>
{
    public ValidateOptionsResult Validate(string? name, SapServiceLayerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            failures.Add(
                $"{SapServiceLayerOptions.SectionName}:BaseUrl must be an absolute HTTPS URL.");
        }

        if (options.RequestTimeoutSeconds is < 1 or > 600)
        {
            failures.Add(
                $"{SapServiceLayerOptions.SectionName}:RequestTimeoutSeconds must be between 1 and 600.");
        }

        if (options.Bases is null || options.Bases.Count == 0)
        {
            failures.Add(
                $"{SapServiceLayerOptions.SectionName}:Bases must contain at least one SAP database.");
        }

        var databaseNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < (options.Bases?.Count ?? 0); index++)
        {
            var sapBase = options.Bases![index];
            var path = $"{SapServiceLayerOptions.SectionName}:Bases:{index}";
            if (sapBase is null)
            {
                failures.Add($"{path} must be an object.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(sapBase.Database))
            {
                failures.Add($"{path}:Database is required.");
            }
            else
            {
                if (!string.Equals(sapBase.Database, sapBase.Database.Trim(), StringComparison.Ordinal))
                {
                    failures.Add($"{path}:Database must not start or end with whitespace.");
                }

                if (!databaseNames.Add(sapBase.Database))
                {
                    failures.Add($"{path}:Database '{sapBase.Database}' is duplicated.");
                }
            }

            if (string.IsNullOrWhiteSpace(sapBase.Description))
            {
                failures.Add($"{path}:Description is required.");
            }
            else if (!string.Equals(sapBase.Description, sapBase.Description.Trim(), StringComparison.Ordinal))
            {
                failures.Add($"{path}:Description must not start or end with whitespace.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
