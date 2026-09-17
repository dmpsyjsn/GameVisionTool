using LLama;
using LLama.Abstractions;
using LLama.Common;
using LLama.Transformers;

namespace GameVisionTool.Integration.LlamaSharp;

/// <summary>
/// Wraps <see cref="PromptTemplateTransformer"/> and appends an already-closed reasoning block to the
/// end of the rendered assistant turn, so the model begins generating with its thinking apparently
/// finished and goes straight to prose.
///
/// The reason this exists: a reasoning model expands its thinking to fill whatever budget it is
/// given. Observed on a 9,000 token cap, roughly 7,100 tokens went into a single &lt;think&gt; block
/// and only ~1,900 reached the story - and when the block does not close before the cap, there is no
/// answer at all, only the model's notes. Raising MaxTokens does not fix that, it feeds it.
///
/// Only worth applying to a model that actually reasons. For a model that has never seen these tags,
/// the prefill is stray text in a position it cannot interpret - see the flag on
/// <see cref="LlamaChatSessionGenerator"/>.
/// </summary>
/// <param name="prefill">
/// The block to append. Defaults to an open-and-closed pair. If the model's own chat template already
/// opens &lt;think&gt; as part of the assistant turn, pass just the closing tag instead - otherwise the
/// prompt ends up with two opening tags.
/// </param>
public sealed class ThinkingPrefillHistoryTransform(LLamaWeights model, string prefill)
    : IHistoryTransform
{
    /// <summary>For a template that does not open the block itself.</summary>
    public const string ClosedThinkingBlock = "<think></think>";

    /// <summary>For a template that already opens the block as part of the assistant turn.</summary>
    public const string ClosingTagOnly = "</think>";

    /// <summary>
    /// The opening tag on its own. Not a prefill - it is what a model's chat template is searched for
    /// to decide whether prefilling means anything to it at all. See
    /// <see cref="LlamaChatSessionGenerator.ModelUsesThinkingTags"/>.
    /// </summary>
    public const string OpenTag = "<think>";

    private readonly PromptTemplateTransformer _inner = new(model, withAssistant: true);

    public string HistoryToText(ChatHistory history) => _inner.HistoryToText(history) + prefill;

    public ChatHistory TextToHistory(AuthorRole role, string text) => _inner.TextToHistory(role, text);

    public IHistoryTransform Clone() => new ThinkingPrefillHistoryTransform(model, prefill);
}
