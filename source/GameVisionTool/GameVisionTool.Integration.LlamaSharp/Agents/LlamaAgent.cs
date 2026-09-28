using LLama.Common;

namespace GameVisionTool.Integration.LlamaSharp.Agents;

/// <summary>
/// Assembles the pieces one generation needs and runs it. This is the code that used to sit in
/// LlamaStoryHandlers; it lives here so <c>Logic</c> can depend on <see cref="ILlamaAgent"/> instead of
/// constructing LlamaSharp types itself.
///
/// Takes the concrete <see cref="LlamaModelProvider"/> rather than an interface, the same way
/// <c>GoogleGeminiAgentResponseGenerator</c> takes <c>GeminiClientBuilder</c>: both sit inside the
/// integration that owns them, and the provider's whole job is to be the one place the weights live.
///
/// Per-call rather than per-agent state. The session and its context are built and disposed inside
/// <see cref="GenerateResponse"/>, so a singleton registration is safe and two pages generating at once
/// do not share a chat history. Only the weights are cached, by the provider.
/// </summary>
public sealed class LlamaAgent(LlamaModelProvider modelProvider) : ILlamaAgent
{
    public async Task<LlamaGenerationResult> GenerateResponse(
        string modelPath,
        uint contextSize,
        int gpuLayerCount,
        int maxTokens,
        float temperature,
        string agentSystemPrompt,
        bool suppressThinking,
        string stopMarker,
        string userMessage,
        ChatHistory? chatHistory)
    {
        var parameters = new LlamaParametersGenerator(modelPath, gpuLayerCount, contextSize).Parameters;

        // Loading weights is a long blocking native call, so it is pushed off the calling thread - which
        // is the UI thread when a generation is started from the Backstory page. A model already loaded
        // returns from the provider's cache and this costs nothing.
        var model = await Task.Run(() => modelProvider.GetOrLoad(modelPath, contextSize, gpuLayerCount));

        using var chatSessionGenerator = new LlamaChatSessionGenerator(model, parameters, suppressThinking);
        using var responseGenerator = new LlamaAgentResponseGenerator(
            chatSessionGenerator, maxTokens, temperature, agentSystemPrompt, stopMarker, chatHistory);

        return await responseGenerator.GetResponse(userMessage);
    }
}
