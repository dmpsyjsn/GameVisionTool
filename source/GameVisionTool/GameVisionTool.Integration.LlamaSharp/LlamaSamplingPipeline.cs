using LLama.Native;
using LLama.Sampling;

namespace GameVisionTool.Integration.LlamaSharp;

/// <summary>
/// <see cref="DefaultSamplingPipeline"/>'s chain with a DRY sampler added, because LLamaSharp 0.27
/// exposes no DRY knob on the default pipeline - only the underlying
/// <see cref="SafeLLamaSamplerChainHandle.AddDry"/> - and DRY has to sit inside the chain rather than
/// after it, so subclassing and appending is not an option.
///
/// Why DRY and not a bigger <see cref="RepeatPenalty"/>: the two catch different things. RepeatPenalty
/// scores individual tokens over a fixed window, so raising it to break a long loop also penalises
/// every ordinary word that recurs in a page of prose - a character's name, "the" - and flattens the
/// writing. DRY matches repeated *sequences* and penalises only the token that would continue one, with
/// a penalty that grows exponentially in the length of the match. A stock phrase costs almost nothing;
/// a paragraph reproduced verbatim for the seventy-seventh time costs effectively everything.
///
/// The chain order below is <see cref="DefaultSamplingPipeline"/>'s exactly, with DRY in llama.cpp's
/// own position for it - after the penalties, before the truncation samplers. Everything except the
/// penalties and DRY keeps the default's values, which are llama.cpp's and suit prose as they are.
/// </summary>
public sealed class LlamaSamplingPipeline : BaseSamplingPipeline
{
    public float Temperature { get; init; } = 0.75f;

    // 1.1 over the last 64 tokens is llama.cpp's own default. Kept alongside DRY rather than replaced
    // by it: this is the cheap guard against a single token stuttering, which DRY's minimum match
    // length lets through by design.
    public float RepeatPenalty { get; init; } = 1.1f;

    public int PenaltyCount { get; init; } = 64;

    public float FrequencyPenalty { get; init; }

    public float PresencePenalty { get; init; }

    public int TopK { get; init; } = 40;

    public float TypicalP { get; init; } = 1f;

    public float TopP { get; init; } = 0.9f;

    public float MinP { get; init; } = 0.1f;

    public int MinKeep { get; init; } = 1;

    public uint Seed { get; init; } = (uint)Random.Shared.Next();

    /// <summary>Penalty multiplier. 0 disables DRY entirely; 0.8 is the designed-for "on" value.</summary>
    public float DryMultiplier { get; init; } = 0.8f;

    /// <summary>Exponential base - how sharply the penalty climbs with match length.</summary>
    public float DryBase { get; init; } = 1.75f;

    /// <summary>Matches longer than this are penalised. Below it, repetition is free.</summary>
    public int DryAllowedLength { get; init; } = 2;

    /// <summary>
    /// How far back to scan. <b>-1 means the whole context; 0 means disabled.</b> Deliberately not a
    /// small window - scanning further back than <see cref="PenaltyCount"/> is the entire reason this
    /// sampler is here. The loop that prompted it repeated a paragraph across roughly 34,000
    /// characters, so every recurrence sat far outside the 64-token penalty window and nothing pushed
    /// the model off it.
    ///
    /// <b>Do not set this to 0.</b> LLamaSharp's own XML doc on
    /// <see cref="SafeLLamaSamplerChainHandle.AddDry"/> documents 0 as "entire context", which is
    /// wrong - llama.cpp reads 0 as "scan nothing" and switches DRY off outright. Taking that comment
    /// at face value shipped a DRY sampler that did nothing at all, and the loop it was added to
    /// prevent recurred as 539 repetitions of "**END OF PROFILE**".
    ///
    /// Verified against the bundled native binary rather than the docs: with the sampler fed an
    /// already-looping context, a 0 here produces output byte-identical to
    /// <see cref="DryMultiplier"/> = 0, while -1 and 512 both break the loop.
    /// </summary>
    public int DryPenaltyLastN { get; init; } = -1;

    /// <summary>
    /// Tokens that repetition is not matched across, so structure the model is supposed to repeat -
    /// bullet markers, headings, quoted speech - is not read as a loop. llama.cpp's own set.
    /// </summary>
    public string[] SequenceBreakers { get; init; } = ["\n", ":", "\"", "*"];

    protected override SafeLLamaSamplerChainHandle CreateChain(SafeLLamaContextHandle context)
    {
        var chain = SafeLLamaSamplerChainHandle.Create(LLamaSamplerChainParams.Default());

        chain.AddPenalties(PenaltyCount, RepeatPenalty, FrequencyPenalty, PresencePenalty);
        chain.AddDry(context.ModelHandle, SequenceBreakers, DryMultiplier, DryBase, DryAllowedLength, DryPenaltyLastN);
        chain.AddTopK(TopK);
        chain.AddTypical(TypicalP, MinKeep);
        chain.AddTopP(TopP, MinKeep);
        chain.AddMinP(MinP, MinKeep);
        chain.AddTemperature(Temperature);
        chain.AddDistributionSampler(Seed);

        return chain;
    }
}
