using Google.GenAI.Types;

namespace GameVisionTool.Integration.GoogleGemini.Agents;

/// <summary>
/// Turns a stored thinking level string into the SDK's <see cref="ThinkingLevel"/>.
///
/// <see cref="ThinkingLevel"/> is an open extensible enum - a struct wrapping a raw string, with
/// <c>FromString</c> and an implicit string conversion that accept <em>anything</em> so the SDK
/// keeps working when the API adds a level it has never heard of. That is the wrong trade for a
/// value typed by hand into a settings form: a typo would sail through here and only fail at the
/// API call, so this matches against the levels the SDK actually knows and rejects the rest.
/// </summary>
public static class ThinkingLevelParser
{
    /// <summary>
    /// Matches case-insensitively, so both the API's raw casing ("MINIMAL") and the friendlier form
    /// shown in the agent form ("Minimal") resolve. A blank value means "not set" and maps to
    /// THINKING_LEVEL_UNSPECIFIED, leaving the choice to the API.
    /// </summary>
    public static bool TryParse(string? value, out ThinkingLevel thinkingLevel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            thinkingLevel = ThinkingLevel.ThinkingLevelUnspecified;
            return true;
        }

        var trimmed = value.Trim();

        foreach (var candidate in ThinkingLevel.AllValues)
        {
            if (string.Equals(candidate.Value, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                thinkingLevel = candidate;
                return true;
            }
        }

        thinkingLevel = ThinkingLevel.ThinkingLevelUnspecified;
        return false;
    }

    /// <summary>The levels the SDK knows about, for error messages and anything that offers a choice.</summary>
    public static IEnumerable<string> KnownLevels => ThinkingLevel.AllValues.Select(x => x.Value);
}
