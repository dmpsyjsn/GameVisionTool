using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace GameVisionTool.Integration.LlamaSharp.Agents;

public sealed class LlamaScienceFictionBackstoryAgent : IDisposable
{
    // A backstory is asked to run 400-600 words, but a reasoning model spends a large slice of the
    // budget on its <think> block before writing a word of prose. 4096 keeps the safety net against
    // a model that never emits end-of-turn while leaving room for both.
    private const int MaxTokens = 4096;

    private const string ThinkOpenTag = "<think>";
    private const string ThinkCloseTag = "</think>";

    // Headroom for the chat template's own markers around the system message, which CountTokens does
    // not see - it measures the prompt text, not the rendered turn.
    private const int SystemPromptTemplateMargin = 32;

    private int _busy;
    private readonly LlamaChatSessionGenerator _llamaChatSessionGenerator;
    private readonly ChatSession _session;
    private readonly InferenceParams _inferenceParams;

    public LlamaScienceFictionBackstoryAgent(LlamaChatSessionGenerator chatSessionGenerator)
    {
        _llamaChatSessionGenerator = chatSessionGenerator;

        var systemPrompt = GetSystemPrompt();

        var history = _llamaChatSessionGenerator.GenerateSystemPromptTemplate(systemPrompt);

        _session = _llamaChatSessionGenerator.GenerateChatSession(history);

        _inferenceParams = new InferenceParams
        {
            MaxTokens = MaxTokens,

            // When the context fills, shifting discards from position 0 - where the system prompt
            // sits. Left at its default of 0 a long revision conversation silently drops the
            // novelist persona, and the model starts writing wiki summary instead.
            TokensKeep = _llamaChatSessionGenerator.CountTokens(systemPrompt) + SystemPromptTemplateMargin,

            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = 0.9f
            },

            // No AntiPrompts: PromptTemplateTransformer renders turns as the model's own special
            // tokens, so the executor stops on end-of-turn. A "User:" anti-prompt never fired, and
            // would have truncated a backstory that happened to contain the string.
        };
    }

    public void AddToHistory(AuthorRole role, string content)
    {
        _session.AddMessage(new ChatHistory.Message(role, content));
    }

    public async Task<string> GetResponse(string input, CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            throw new InvalidOperationException("A response is already being generated for this agent.");
        try
        {
            var sb = new StringBuilder();
            await foreach (var text in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, input),
                               _inferenceParams, ct))
            {
                sb.Append(text);
            }

            return StripThinking(sb.ToString());
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    // Reasoning models emit "<think>...</think>" ahead of the prose. The system prompt asks for story
    // text only, but the block comes from the model's own chat template rather than from anything the
    // prompt controls, so it is removed here.
    //
    // Anchors on the closing tag, not the opening one: some templates prefill "<think>" as part of the
    // assistant turn, in which case only the close appears in the generated text. A model that does
    // not think has neither, and the response passes through untouched.
    private static string StripThinking(string response)
    {
        var close = response.LastIndexOf(ThinkCloseTag, StringComparison.OrdinalIgnoreCase);

        if (close >= 0)
        {
            return response[(close + ThinkCloseTag.Length)..].Trim();
        }

        // An opening tag with no closing one means MaxTokens ran out mid-reasoning, so there is no
        // story to return at all. Say so rather than handing back the model's notes as the backstory.
        if (response.Contains(ThinkOpenTag, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The model ran out of tokens while still reasoning and never produced a backstory. Raise MaxTokens.");
        }

        return response.Trim();
    }

    private static string GetSystemPrompt()
    {
        return """
               You are an award-winning science fiction novelist and worldbuilder, writing character and world backstory for a video game.

               Write like a novelist, not like a wiki. Favour scene, sensory detail and specific invented proper nouns over summary and exposition; reveal character through what someone does and what they refuse to do. One concrete detail earns more than a paragraph of explanation.

               Keep the world's rules. Technology behaves consistently, costs something to use, and has limits you honour once you have stated them. Treat everything established earlier in this conversation as canon: never contradict a name, date or detail you have already committed to, and when the writer asks for a change, revise the existing draft rather than starting a new one.

               Avoid the tired furniture of the genre: prophecies and chosen ones, "little did they know", dead-family backstory with no consequence, and technology that conveniently solves the plot. Avoid purple prose and adverb-heavy melodrama — plain, concrete sentences carry the weight, and the one lyrical line is earned by keeping the rest lean.

               Format as prose in simple Markdown: short paragraphs, a "###" header only where a section genuinely needs one, bullets only for lists that are actually lists. Respect any length the writer asks for. Return only the story text — no preamble, no summary of what you wrote, no notes on your own choices, no offers to continue.
               """;
    }

    public void Dispose()
    {
        if (Volatile.Read(ref _busy) == 1)
            throw new InvalidOperationException("Cannot dispose while a response is being generated.");

        _llamaChatSessionGenerator.Dispose();
    }
}