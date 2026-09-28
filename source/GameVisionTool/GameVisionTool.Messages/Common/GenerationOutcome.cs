namespace GameVisionTool.Messages.Common;

/// <summary>
/// The provider-agnostic shape a backstory generation ends at. Both the Llama and the Gemini paths
/// produce the same thing - the text, whether the model ran out of budget partway through it, and the
/// chat history the next turn continues from - so callers can hold one type regardless of which
/// provider ran.
/// </summary>
public sealed record GenerationOutcome(string Text, bool WasTruncated);
