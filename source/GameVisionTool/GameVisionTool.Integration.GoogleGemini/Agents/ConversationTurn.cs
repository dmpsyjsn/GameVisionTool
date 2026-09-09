namespace GameVisionTool.Integration.GoogleGemini.Agents;

// Who authored a conversation turn. Maps to Gemini's role strings ("user" / "model").
public enum ConversationRole
{
    User,
    Model,
}

// A single persisted turn in a conversation. Callers store these (keyed by conversation) and replay
// them on each request, since the Gemini API is stateless and keeps no server-side history.
public class ConversationTurn
{
    public ConversationRole Role { get; set; }

    public string Text { get; set; } = string.Empty;
}

internal static class ConversationRoleExtensions
{
    // Gemini expects the role strings "user" and "model" (note: "model", not "assistant").
    public static string ToGeminiRole(this ConversationRole role) => role switch
    {
        ConversationRole.User => "user",
        ConversationRole.Model => "model",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown conversation role."),
    };

    public static ConversationRole ToConversationRole(this string geminiRole) => geminiRole switch
    {
        "user" => ConversationRole.User,
        "model" => ConversationRole.Model,
        _ => throw new ArgumentOutOfRangeException(nameof(geminiRole), geminiRole, "Unknown Gemini role."),
    };
}
