using System.Text;
using Microsoft.Data.SqlClient;

namespace Floudy.API.Utility;

public static class ConnectionStringNormalizer
{
    private static readonly Dictionary<string, string> KeyAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["userid"] = "User ID",
        ["user id"] = "User ID",
        ["user"] = "User ID",
        ["username"] = "User ID",
        ["uid"] = "User ID",
        ["password"] = "Password",
        ["pwd"] = "Password",
        ["server"] = "Data Source",
        ["host"] = "Data Source",
        ["addr"] = "Data Source",
        ["address"] = "Data Source",
        ["database"] = "Initial Catalog",
        ["db"] = "Initial Catalog",
        ["initial catalog"] = "Initial Catalog",
    };

    private static readonly HashSet<string> IgnoredKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Port",
        "Ssl Mode",
        "SSL Mode",
        "Trust Server Certificate",
        "Pooling",
        "Minimum Pool Size",
        "Maximum Pool Size",
        "Connection Lifetime",
        "Timeout",
        "Command Timeout",
    };

    public static string ToSqlServer(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        var normalized = Normalize(connectionString.Trim());
        _ = new SqlConnectionStringBuilder(normalized);
        return normalized;
    }

    private static string Normalize(string connectionString)
    {
        if (connectionString.Contains("://", StringComparison.Ordinal))
        {
            return NormalizeUri(connectionString);
        }

        var parts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? pendingPort = null;
        string? pendingHost = null;

        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = segment.IndexOf('=');
            if (separator <= 0) continue;

            var key = segment[..separator].Trim();
            var value = segment[(separator + 1)..].Trim();

            if (IgnoredKeys.Contains(key))
            {
                if (key.Equals("Port", StringComparison.OrdinalIgnoreCase)) pendingPort = value;
                continue;
            }

            if (KeyAliases.TryGetValue(key, out var mapped)) key = mapped;

            if (key.Equals("Data Source", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Server", StringComparison.OrdinalIgnoreCase))
            {
                pendingHost = value;
                key = "Data Source";
            }

            parts[key] = value;
        }

        if (pendingHost != null && pendingPort != null && !pendingHost.Contains(','))
        {
            parts["Data Source"] = $"{pendingHost},{pendingPort}";
        }

        return string.Join(';', parts.Select(static pair => $"{pair.Key}={pair.Value}"));
    }

    private static string NormalizeUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            throw new ArgumentException("Connection string URI is not valid.", nameof(uri));
        }

        var builder = new StringBuilder();

        if (!string.IsNullOrEmpty(parsed.Host))
        {
            var host = parsed.Host;
            if (parsed.Port > 0) host = $"{host},{parsed.Port}";
            builder.Append($"Data Source={host};");
        }

        var database = parsed.AbsolutePath.Trim('/');
        if (!string.IsNullOrEmpty(database))
        {
            builder.Append($"Initial Catalog={database};");
        }

        if (!string.IsNullOrEmpty(parsed.UserInfo))
        {
            var credentials = parsed.UserInfo.Split(':', 2);
            if (credentials.Length > 0 && !string.IsNullOrEmpty(credentials[0]))
            {
                builder.Append($"User ID={Uri.UnescapeDataString(credentials[0])};");
            }

            if (credentials.Length > 1)
            {
                builder.Append($"Password={Uri.UnescapeDataString(credentials[1])};");
            }
        }

        var query = parsed.Query.TrimStart('?');
        if (!string.IsNullOrEmpty(query))
        {
            foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = segment.IndexOf('=');
                if (separator <= 0) continue;

                var key = Uri.UnescapeDataString(segment[..separator]);
                var value = Uri.UnescapeDataString(segment[(separator + 1)..]);

                if (KeyAliases.TryGetValue(key, out var mapped)) key = mapped;
                if (!IgnoredKeys.Contains(key))
                {
                    builder.Append($"{key}={value};");
                }
            }
        }

        return builder.ToString().TrimEnd(';');
    }
}
