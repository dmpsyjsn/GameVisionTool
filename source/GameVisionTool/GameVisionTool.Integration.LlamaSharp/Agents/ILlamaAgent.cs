using LLama.Common;

namespace GameVisionTool.Integration.LlamaSharp.Agents;

/// <summary>
/// One local-model generation, start to finish: weights, context, session and sampling are all set up
/// and torn down behind this call. The mirror of <c>IGoogleGeminiAgent</c> on the cloud side - a handler
/// asks for a response and gets one, without naming a LlamaSharp type.
///
/// The parameter list is flat rather than an options object to match the Gemini interface, and because
/// every value on it is already a separate field on the query the handler receives.
/// </summary>
public interface ILlamaAgent
{
    /// <param name="modelPath">Path to the .gguf. Checked for existence upstream by LlamaModelFileDecorator.</param>
    /// <param name="suppressThinking">
    /// Prefills a closed reasoning block so a thinking model goes straight to prose. Only correct for a
    /// model whose chat template actually uses the tags - see <see cref="LlamaChatSessionGenerator"/>.
    /// </param>
    /// <param name="stopMarker">
    /// The string this agent's system prompt is told to end with, registered as a stop sequence. Empty
    /// disables the mechanism - see <see cref="LlamaAgentResponseGenerator"/> for why it matters.
    /// </param>
    Task<LlamaGenerationResult> GenerateResponse(
        string modelPath,
        uint contextSize,
        int gpuLayerCount,
        int maxTokens,
        float temperature,
        string agentSystemPrompt,
        bool suppressThinking,
        string stopMarker,
        string userMessage,
        ChatHistory? chatHistory);
}
