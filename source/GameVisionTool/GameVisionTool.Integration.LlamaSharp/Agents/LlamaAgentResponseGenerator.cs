using System.Text;
using LLama;
using LLama.Common;
using Serilog;

namespace GameVisionTool.Integration.LlamaSharp.Agents;

/// <param name="Text">The response, with any reasoning block already stripped.</param>
/// <param name="WasTruncated">
/// True when generation stopped because it ran into MaxTokens rather than because the model reached
/// the end of its turn - meaning <paramref name="Text"/> stops mid-sentence and is not a whole answer.
/// </param>
public sealed record LlamaGenerationResult(string Text, bool WasTruncated);

public sealed class LlamaAgentResponseGenerator : IDisposable
{
    private const string ThinkOpenTag = "<think>";
    private const string ThinkCloseTag = "</think>";

    // Share of MaxTokens at which a response counts as having run into the cap. A ratio rather than a
    // fixed token slack because the round-trip in WasTruncated undercounts - see the note there.
    private const double TruncationRatio = 0.95;

    // Headroom for the chat template's own markers around the system message, which CountTokens does
    // not see - it measures the prompt text, not the rendered turn.
    private const int SystemPromptTemplateMargin = 32;

    private int _busy;
    private readonly LlamaChatSessionGenerator _llamaChatSessionGenerator;
    private readonly ChatSession _session;
    private readonly InferenceParams _inferenceParams;
    private readonly int _maxTokens;
    private readonly string _stopMarker;

    /// <param name="stopMarker">
    /// The string this agent's system prompt instructs the model to write when it has finished,
    /// registered as a stop sequence so generation actually ends there. Empty disables the mechanism.
    ///
    /// The marker is the only reliable brake on a model that does not emit EOS. Three captured
    /// runaways all had the same shape: a complete answer, then generation carrying on to the token
    /// cap - writing the user's half of an imaginary conversation, then repeating itself. Nothing in
    /// the sampler can fix that, because every sampler decides *which* token comes next, never
    /// *whether* one should. A stop sequence is the only layer that ends the loop rather than
    /// redecorating it: with DRY active the repetition merely changed from a block repeated verbatim
    /// 539 times to fifty paraphrases of the same paragraph, over the same 105 seconds and the same
    /// 8,192 tokens.
    ///
    /// Asking for the marker in the prompt without registering it here is the trap, and it has already
    /// been fallen into: an earlier prompt ended with "**END OF PROFILE**" and nothing watched for it,
    /// so the model wrote it 539 times. It gave the model a way to *say* it had finished without any
    /// way to actually stop.
    ///
    /// Per-agent rather than one constant for the app, because it has to match wording that lives in
    /// the agent's own system prompt. Pick something prose will never contain - the matching is a plain
    /// substring test on decoded text, so a marker like "END" would cut a response off at the first
    /// sentence that happened to use the word.
    /// </param>
    public LlamaAgentResponseGenerator(
        LlamaChatSessionGenerator chatSessionGenerator,
        int maxTokens,
        float temperature,
        string agentSystemPrompt,
        string stopMarker = "")
    {
        _llamaChatSessionGenerator = chatSessionGenerator;
        _maxTokens = maxTokens;

        // Whitespace-only is treated as absent rather than registered: an all-whitespace stop sequence
        // matches almost immediately and would end every generation a few tokens in.
        _stopMarker = string.IsNullOrWhiteSpace(stopMarker) ? string.Empty : stopMarker.Trim();

        WarnIfThinkingPrefillIsWasted();
        WarnIfNoStopMarker();

        // Normalised once, then used for both the rendered turn and the token count below, so the
        // budget is measured against the text the model actually receives.
        var systemPrompt = NormalizeLineEndings(agentSystemPrompt);

        var history = _llamaChatSessionGenerator.GenerateSystemPromptTemplate(systemPrompt);

        _session = _llamaChatSessionGenerator.GenerateChatSession(history);
        
        _inferenceParams = new InferenceParams
        {
            MaxTokens = maxTokens,

            // When the context fills, shifting discards from position 0 - where the system prompt
            // sits. Left at its default of 0 a long revision conversation silently drops the
            // novelist persona, and the model starts writing wiki summary instead.
            TokensKeep = _llamaChatSessionGenerator.CountTokens(systemPrompt) + SystemPromptTemplateMargin,

            // Only Temperature comes from the agent - LlamaSamplingPipeline owns the rest, and is
            // DefaultSamplingPipeline's chain with a DRY sampler added. The two repetition guards it
            // carries cover different distances, which is why it keeps both: RepeatPenalty scores
            // single tokens over a 64-token window, DRY matches repeated sequences across the whole
            // context. A paragraph reproduced verbatim pages later is invisible to the first and is
            // exactly what the second is for.
            SamplingPipeline = new LlamaSamplingPipeline
            {
                Temperature = temperature,
            },

            // The agent's stop marker, or nothing when it has none.
            //
            // This is deliberately not a guess at a turn delimiter. An earlier "User:" anti-prompt was
            // removed because it never fired - PromptTemplateTransformer renders turns as the model's
            // own special tokens, so that string never marks a boundary - and because it would have
            // cut short any response that happened to contain the word. The runaways emitted no
            // delimiter at all, only prose impersonating one, so there was nothing to match on.
            //
            // A marker the system prompt asks for is the opposite case: a string chosen precisely
            // because it appears nowhere else, that the model has been told to write, and that the
            // executor is now told to stop on.
            AntiPrompts = HasStopMarker ? [_stopMarker] : [],
        };
    }

    private bool HasStopMarker => _stopMarker.Length > 0;

    /// <summary>
    /// Reports an agent with no stop marker. Not an error - it is the setting every agent started with,
    /// and a model that ends its turn cleanly never needs one - but it is the difference between a
    /// runaway that stops and one that fills the token cap, so it is worth a line in the log when a
    /// generation later turns out to have been cut off.
    /// </summary>
    private void WarnIfNoStopMarker()
    {
        if (HasStopMarker)
            return;

        Log.Debug(
            "This agent has no stop marker, so the only thing that can end generation is the model emitting " +
            "end-of-turn. If it does not, generation runs to the {MaxTokens} token cap and repeats itself to " +
            "get there. Give the agent a stop marker and tell its system prompt to end with it",
            _maxTokens);
    }
    
    /// <summary>
    /// Reports an agent whose Suppress Thinking is on against a model that has no idea what
    /// <c>&lt;think&gt;</c> is. The prefill then lands as ordinary text exactly where the model's reply
    /// should begin, and the failure it causes is invisible downstream: the tags never appear in the
    /// generated text either way, so nothing about the response distinguishes this from a clean run.
    /// Checked here, before generating, rather than inferred afterwards.
    ///
    /// A warning rather than a throw. The model usually still answers - what it then fails to do is
    /// stop, so the usable answer arrives with pages of runaway generation stapled to it. Discarding
    /// that would be worse than handing it over with the reason logged.
    /// </summary>
    private void WarnIfThinkingPrefillIsWasted()
    {
        if (!_llamaChatSessionGenerator.SuppressThinking || _llamaChatSessionGenerator.ModelUsesThinkingTags)
            return;

        Log.Warning(
            "This agent has Suppress Thinking on, but the loaded model does not use {ThinkTag} tags - its chat " +
            "template never writes one and its tokenizer does not carry one. The prefill is stray text where the " +
            "reply should start, which can leave the model generating past the end of its turn and repeating " +
            "itself until it hits the {MaxTokens} token cap. Turn Suppress Thinking off for this agent.",
            ThinkOpenTag,
            _maxTokens);
    }

    /// <summary>
    /// Rewrites CRLF and lone CR to LF. Applied to every piece of text on its way into the model,
    /// because MAUI's <c>Editor</c> on Windows ends lines with a bare CR and nothing downstream fixes
    /// it - a system prompt typed as structured markdown arrives as one run-on blob.
    ///
    /// It matters more than a stray control character usually would, because of how byte-level BPE
    /// treats the two. In this Mistral vocabulary LF appears in 1,069 tokens, merged into the forms
    /// that carry document structure - a newline, a paragraph break, a newline after a full stop.
    /// CR appears in exactly one: itself, unmerged. So CR-delimited text is not "structure the model
    /// reads slightly differently", it is structure the model never sees, replaced by a token it has
    /// almost no training signal for.
    ///
    /// Done here rather than on save so that agents already stored with CR line endings are fixed on
    /// read, and so the user's brief - which is never persisted - is covered by the same pass.
    /// </summary>
    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n');

    public void AddToHistory(AuthorRole role, string content)
    {
        _session.AddMessage(new ChatHistory.Message(role, NormalizeLineEndings(content)));
    }
    
    public async Task<LlamaGenerationResult> GetResponse(string input, CancellationToken ct = default)
    {
        if (Interlocked.Exchange(ref _busy, 1) == 1)
            throw new InvalidOperationException("A response is already being generated for this agent.");
        try
        {
            var sb = new StringBuilder();
            await foreach (var text in _session.ChatAsync(
                               new ChatHistory.Message(AuthorRole.User, NormalizeLineEndings(input)),
                               _inferenceParams, ct))
            {
                sb.Append(text);
            }

            var raw = sb.ToString();

            // The executor yields decoded text before it checks for a stop sequence, so the marker
            // itself arrives in the stream and has to be cut off here - see CutAtStopMarker.
            var stoppedOnMarker = CutAtStopMarker(raw, out var answer);

            // A marker that arrived is an authoritative stop reason, and the only one LLamaSharp
            // offers. The model wrote the string it was told to write at the end, so the answer is
            // whole however close to the cap it landed - no need to consult the heuristic below, which
            // exists only because there is normally nothing better to go on.
            //
            // Measured on the raw text, before StripThinking: the reasoning block was generated too,
            // and it is what pushes a thinking model into the cap in the first place.
            var wasTruncated = !stoppedOnMarker && WasTruncated(raw);

            if (wasTruncated)
            {
                Log.Warning(
                    "Llama response hit the {MaxTokens} token cap and was cut off mid-response. Consider raising Max Tokens for this agent",
                    _maxTokens);
            }

            return new LlamaGenerationResult(StripThinking(answer), wasTruncated);
        }
        finally
        {
            Interlocked.Exchange(ref _busy, 0);
        }
    }

    /// <summary>
    /// Removes the stop marker and everything after it, reporting whether one was found.
    ///
    /// The marker has to be cut here rather than filtered out of the stream, because of the order
    /// <c>StatefulExecutorBase.InferAsync</c> works in: each batch of tokens is decoded and yielded to
    /// the caller *before* <c>PostProcess</c> tests it against the stop sequences. By the time the
    /// executor decides to stop, the text that triggered it has already been handed over. So a stop
    /// sequence in LLamaSharp ends generation but does not hide the match - the caller sees it and has
    /// to trim it.
    ///
    /// Cuts at the first occurrence. Generation stops at the first match, so a second one can only
    /// exist if the model wrote the marker before the executor's next check, and the answer still
    /// ended at the first.
    ///
    /// Ordinal, not culture-aware: the marker is a fixed token chosen to be unique, not text in a
    /// language, and a culture-sensitive search can match strings that are merely equivalent.
    /// </summary>
    private bool CutAtStopMarker(string rawResponse, out string text)
    {
        if (HasStopMarker)
        {
            var at = rawResponse.IndexOf(_stopMarker, StringComparison.Ordinal);

            if (at >= 0)
            {
                text = rawResponse[..at];
                return true;
            }

            Log.Debug(
                "Llama response ended without the agent's {StopMarker} stop marker, so generation stopped for " +
                "some other reason - end-of-turn, or the token cap",
                _stopMarker);
        }

        text = rawResponse;
        return false;
    }

    /// <summary>
    /// LLamaSharp surfaces no stop reason - <c>ChatAsync</c> yields text and then simply stops, whether
    /// the model reached the end of its turn or ran into MaxTokens. Neither the executor nor the session
    /// exposes which it was, so the only signal left is how much came back: a response that used its
    /// entire budget almost certainly ran out rather than finished.
    ///
    /// A fallback, not the first choice. An agent with a stop marker gets a real answer instead - the
    /// marker's presence says the model finished - and this heuristic is only consulted when no marker
    /// arrived. See <see cref="CutAtStopMarker"/>.
    ///
    /// Re-tokenising decoded text does not reproduce the token sequence that produced it, and the error
    /// is not symmetric: it undercounts. Tokens are sampled one at a time, while re-tokenising applies
    /// greedy BPE to the finished string and finds merges the sampler never used. The more repetitive
    /// the text, the more merges are available and the larger the gap.
    ///
    /// This used to allow a fixed slack of a few tokens, on the assumption the round-trip was near
    /// exact and that erring meant a spurious warning. It is not near exact. A measured case: a
    /// response capped at 8,192 tokens re-tokenised to 8,071 - 121 tokens short, 98.52% of the cap -
    /// and sailed under a threshold set 8 tokens below it. The response was cut off mid-word and the
    /// user was told nothing, which is the direction that actually costs something.
    ///
    /// Hence a share of the budget rather than a fixed slack. <see cref="TruncationRatio"/> sits about
    /// three times further out than that measured gap. The remaining failure mode is the harmless one:
    /// a spurious warning on a response that genuinely finished inside the last few percent of its
    /// budget.
    /// </summary>
    private bool WasTruncated(string rawResponse)
    {
        // LLamaSharp treats a non-positive MaxTokens as "no limit", so there is no cap to hit.
        if (_maxTokens <= 0) return false;

        return _llamaChatSessionGenerator.CountGeneratedTokens(rawResponse) >= _maxTokens * TruncationRatio;
    }

    // Reasoning models emit "<think>...</think>" ahead of the prose. The system prompt asks for story
    // text only, but the block comes from the model's own chat template rather than from anything the
    // prompt controls, so it is removed here.
    //
    // Anchors on the closing tag, not the opening one: some templates prefill "<think>" as part of the
    // assistant turn, in which case only the close appears in the generated text.
    //
    // Neither tag present is the normal outcome and says nothing about the model. It is what a
    // non-thinking model produces, and equally what a thinking model produces once SuppressThinking
    // has prefilled a closed block - the prefill goes into the prompt, so the generated text resumes
    // after it and carries no tags at all. Do not read an absence here as a mismatched model; that
    // check has to happen against the model itself, and does, in WarnIfThinkingPrefillIsWasted.
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
                "The model ran out of tokens while still reasoning and never produced a backstory. Raise Max Tokens for this model.");
        }

        return response.Trim();
    }
    
    public void Dispose()
    {
        if (Volatile.Read(ref _busy) == 1)
            throw new InvalidOperationException("Cannot dispose while a response is being generated.");

        _llamaChatSessionGenerator.Dispose();
    }
}