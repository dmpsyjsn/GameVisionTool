using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.AgentSettings;

public sealed class LlamaAgentSettings : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public AgentGroupType GroupType { get; set; } = AgentGroupType.Backstory;
    public string SystemPrompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
    public float Temperature { get; set; } = 0.9f;
    // Off by default: wrong for most local models, and the safe direction when it is wrong either way
    // - see the suppressThinking param on LlamaChatSessionGenerator.
    public bool SuppressThinking { get; set; }

    // The string this agent's system prompt tells the model to write when it has finished, registered
    // as a stop sequence so generation actually ends there. Empty means the agent does not use one.
    // Per-agent rather than a global constant because it has to match whatever wording the prompt uses
    // - see the stopMarker param on LlamaAgentResponseGenerator.
    //
    // Coerced to empty rather than left as an auto-property, because "no marker" is the common case and
    // LiteDB does not round-trip it: BsonMapper.Global.EmptyStringToNull defaults to true, so an empty
    // string is stored as BSON null and read back as null. The UI calls Trim() on whatever comes back,
    // so without this an agent saved with a blank marker throws the moment it is opened for editing.
    // Fixed here rather than by turning EmptyStringToNull off, which would change how every string on
    // every entity is persisted to fix one field's invariant.
    //
    // Note an *absent* field behaves differently and needs no help: LiteDB leaves it at the initializer
    // below, which is why agents stored before this field existed already read back as empty.
    public string StopMarker
    {
        get => field;
        set => field = value ?? string.Empty;
    } = string.Empty;
}
