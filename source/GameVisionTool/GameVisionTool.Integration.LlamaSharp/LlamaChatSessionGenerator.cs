using System.Text;
using LLama;
using LLama.Common;
using LLama.Transformers;

namespace GameVisionTool.Integration.LlamaSharp;

/// <param name="suppressThinking">
/// Prefills a closed reasoning block into the assistant turn so a thinking model goes straight to
/// prose - see <see cref="ThinkingPrefillHistoryTransform"/> for why. Comes from the agent
/// (LlamaAgentSettings.SuppressThinking), since it is only correct for a model whose chat template
/// actually uses the tags.
///
/// Leave it false for a model that does not - including one that reasons in plain prose rather than in
/// tagged blocks. There the prefill is text the model has never seen, sitting exactly where its reply
/// should begin, and it can generate past the end of its turn instead of emitting EOS: observed with a
/// Mistral-Nemo thinking finetune, which finished a good answer, then wrote both halves of an
/// imaginary follow-up conversation and looped a paragraph until it hit MaxTokens.
///
/// Off by default, for two reasons. Most local models do not use the tags - eight of the eleven .gguf
/// files on this machine do not, see <see cref="ModelUsesThinkingTags"/> - and the two ways of being
/// wrong are not equally bad. Off against a thinking model costs tokens, since it reasons in the open
/// and eats into MaxTokens; on against a model that does not think is the runaway above. Wrong in the
/// cheap direction by default, with <see cref="ModelUsesThinkingTags"/> reporting when it looks wrong
/// in either.
/// </param>
public sealed class LlamaChatSessionGenerator(
    LLamaWeights model,
    ModelParams parameters,
    bool suppressThinking = false) : IDisposable
{
    // The GGUF key holding the model's own Jinja chat template, if it shipped with one.
    private const string ChatTemplateMetadataKey = "tokenizer.chat_template";

    private readonly LLamaContext _context = model.CreateContext(parameters);

    /// <summary>What the caller asked for, so a consumer can tell whether the prefill went in.</summary>
    public bool SuppressThinking => suppressThinking;

    /// <summary>
    /// Whether <c>&lt;think&gt;</c> means anything to the loaded model, so a mismatched
    /// <see cref="SuppressThinking"/> can be reported rather than silently producing a bad generation.
    ///
    /// Two independent signals, either of which is enough: the template writes the tag, or the
    /// tokenizer carries it as a single special token. Checked across the eleven .gguf files on this
    /// machine, Qwen3 and Qwen3.6 hit both, DeepSeek-R1-Distill-Qwen hits only the second - its
    /// template never writes a tag - and the remaining eight (Mistral-Nemo, Phi-3, Phi-4, llama-2,
    /// phi-2, thespis) hit neither. So the vocabulary check is what does the work in practice; the
    /// template check is kept for a finetune that writes the tag as plain text without a vocabulary
    /// entry, which is a real shape and costs nothing to test for. Dropping either one alone would
    /// have raised a false alarm on DeepSeek-R1.
    ///
    /// Read from the loaded model every time rather than stored. It is a fact about the model, and
    /// agent and model are chosen independently at generation time - a copy cached against either one
    /// would be right only for the pairing that happened to write it.
    ///
    /// Deliberately not used to override <see cref="SuppressThinking"/>. A heuristic that silently
    /// countermands a setting the user ticked is worse than one that tells them it looks wrong.
    /// </summary>
    public bool ModelUsesThinkingTags { get; } =
        (model.Metadata.TryGetValue(ChatTemplateMetadataKey, out var chatTemplate)
         && chatTemplate.Contains(ThinkingPrefillHistoryTransform.OpenTag, StringComparison.OrdinalIgnoreCase))
        || model.Tokenize(ThinkingPrefillHistoryTransform.OpenTag, add_bos: false, special: true, Encoding.UTF8).Length == 1;

    public ChatSession GenerateChatSession(ChatHistory chatHistory)
    {
        var executor = new InteractiveExecutor(_context);

        var session = new ChatSession(executor, chatHistory);

        // add the default templator. If llama.cpp doesn't support the template by default,
        // you'll need to write your own transformer to format the prompt correctly
        //
        // The thinking variant wraps that same templator rather than replacing it, so the model's own
        // chat template still renders every turn - it only adds the prefill on the end.
        session.WithHistoryTransform(suppressThinking
            ? new ThinkingPrefillHistoryTransform(model, ThinkingPrefillHistoryTransform.ClosedThinkingBlock)
            : new PromptTemplateTransformer(model, withAssistant: true));

        // Strip the stray replacement character llama 3 emits around its end-of-turn token. "User:"
        // used to be filtered here too, but PromptTemplateTransformer renders turns as the model's
        // own special tokens - so that string never marks a turn boundary, and filtering it would
        // only corrupt a backstory that legitimately contained the word.
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            ["�"],
            redundancyLength: 5));

        return session;
    }

    // Token count for the loaded model's own tokenizer, used to work out how much of the prompt has
    // to survive context shifting.
    public int CountTokens(string text) => _context.Tokenize(text, addBos: true, special: true).Length;

    // Same tokenizer, without the begin-of-sequence marker. Generated text is not a prompt and never
    // carries one, so counting it with addBos would overstate the length by a token - which matters
    // when the count is being compared against a generation cap.
    public int CountGeneratedTokens(string text) => _context.Tokenize(text, addBos: false, special: true).Length;

    public ChatHistory GenerateSystemPromptTemplate(string template)
    {
        var history = new ChatHistory();
        history.AddMessage(AuthorRole.System, template);
        return history;
    }

    public void Dispose() => _context.Dispose();
}