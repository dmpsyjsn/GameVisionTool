using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

/// <summary>
/// Serilog destructuring policy that redacts secret-bearing properties before they reach a sink.
///
/// The logging decorators destructure whole messages and results (<c>{@command}</c>,
/// <c>{@query}</c>, <c>{@returnResult}</c>), which would otherwise write API keys into the
/// rolling log file - the one artifact users routinely hand to other people in bug reports.
/// This keeps the rest of the object graph intact so the logs stay useful for diagnostics.
///
/// Register with <c>.Destructure.With&lt;SensitiveDataDestructuringPolicy&gt;()</c>.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public const string RedactedPlaceholder = "***REDACTED***";
    private const string UnreadablePlaceholder = "***UNREADABLE***";

    /// <summary>Redacted when the property name contains any of these (case-insensitive).</summary>
    private static readonly string[] SensitiveNameFragments =
    [
        "apikey", "api_key", "secret", "password", "passphrase", "pwd",
        "token", "credential", "connectionstring"
    ];

    /// <summary>
    /// Redacted on an exact name match only. "key" is deliberately not a fragment match -
    /// that would also redact "Monkey", "Keyboard" and "KeyCount".
    /// </summary>
    private static readonly string[] SensitiveExactNames = ["key", "apikeys"];

    /// <summary>
    /// Checked first, and wins over everything below. "token" has to stay a fragment because real
    /// secrets carry it as a suffix (AccessToken, RefreshToken), but an LLM generation budget
    /// carries it too: MaxTokens is a length limit, not a credential. Redacting it hid the value
    /// needed to diagnose a real truncation bug - every response was silently hitting the cap, and
    /// the log said "***REDACTED***" where the cap should have been.
    ///
    /// Exemptions are exact-name only, so an unanticipated "...Token" property still fails closed.
    /// </summary>
    private static readonly string[] NonSensitiveExactNames = ["maxtokens", "tokencount"];

    // Null value means "this type has nothing sensitive on it" - hand it back to Serilog untouched.
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]?> PropertyCache = new();

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        result = null!;

        if (value is null) return false;

        var type = value.GetType();

        // Leave scalars, strings and collections to Serilog's own handling. Policies are re-applied
        // to each element of a sequence, so view models nested inside arrays are still covered.
        if (type.IsPrimitive || type.IsEnum || value is string || value is IEnumerable) return false;

        var properties = PropertyCache.GetOrAdd(type, ResolveProperties);

        if (properties is null) return false;

        var logEventProperties = new List<LogEventProperty>(properties.Length);

        foreach (var property in properties)
        {
            object? rawValue;

            try
            {
                rawValue = property.GetValue(value);
            }
            catch (Exception)
            {
                // A throwing getter must not take down the logging pipeline.
                logEventProperties.Add(new LogEventProperty(property.Name, new ScalarValue(UnreadablePlaceholder)));
                continue;
            }

            var propertyValue = ShouldRedact(property.Name, rawValue)
                ? new ScalarValue(RedactedPlaceholder)
                : propertyValueFactory.CreatePropertyValue(rawValue, destructureObjects: true);

            logEventProperties.Add(new LogEventProperty(property.Name, propertyValue));
        }

        result = new StructureValue(logEventProperties, type.Name);
        return true;
    }

    /// <summary>
    /// An absent or empty secret is kept as-is: knowing the field was blank is useful when
    /// diagnosing "my key isn't working" and discloses nothing.
    /// </summary>
    private static bool ShouldRedact(string propertyName, object? rawValue)
    {
        if (!IsSensitiveName(propertyName)) return false;

        return rawValue switch
        {
            null => false,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => true
        };
    }

    private static bool IsSensitiveName(string propertyName)
    {
        foreach (var exemptName in NonSensitiveExactNames)
        {
            if (propertyName.Equals(exemptName, StringComparison.OrdinalIgnoreCase)) return false;
        }

        foreach (var exactName in SensitiveExactNames)
        {
            if (propertyName.Equals(exactName, StringComparison.OrdinalIgnoreCase)) return true;
        }

        foreach (var fragment in SensitiveNameFragments)
        {
            if (propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <summary>
    /// Returns every readable public instance property when the type carries at least one
    /// sensitive property, otherwise null so Serilog's default destructuring is used.
    /// </summary>
    private static PropertyInfo[]? ResolveProperties(Type type)
    {
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.CanRead && x.GetIndexParameters().Length == 0)
            .ToArray();

        return properties.Any(x => IsSensitiveName(x.Name)) ? properties : null;
    }
}
