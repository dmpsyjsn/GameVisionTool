using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Queries;

namespace GameVisionTool.Messages.Queries.Agents;

public class GetLlamaAgents : IQuery<AgentsViewModel>;

public class GetLlamaAgent(Guid id) : IQuery<AgentViewModel>
{
    public Guid Id { get; } = id;
}

public class AgentsViewModel(AgentViewModel[] items)
{
    public AgentViewModel[] Items { get; } = items;
}

public class AgentViewModel(Guid id, string name, AgentGroupType groupType, string systemPrompt, int maxTokens, float temperature, bool suppressThinking, string stopMarker)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public AgentGroupType GroupType { get; } = groupType;
    public string SystemPrompt { get; } = systemPrompt;
    public int MaxTokens { get; } = maxTokens;
    public float Temperature { get; } = temperature;
    public bool SuppressThinking { get; } = suppressThinking;
    public string StopMarker { get; } = stopMarker;
}
