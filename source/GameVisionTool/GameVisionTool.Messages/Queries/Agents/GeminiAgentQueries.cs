using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Queries;

namespace GameVisionTool.Messages.Queries.Agents;

public class GetGeminiAgents : IQuery<GeminiAgentsViewModel>;

public class GetGeminiAgent(Guid id) : IQuery<GeminiAgentViewModel>
{
    public Guid Id { get; } = id;
}

public class GeminiAgentsViewModel(GeminiAgentViewModel[] items)
{
    public GeminiAgentViewModel[] Items { get; } = items;
}

public class GeminiAgentViewModel(Guid id, string name, AgentGroupType groupType, string model, string systemInstructions, int maxTokens, string thinkingLevel)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public AgentGroupType GroupType { get; } = groupType;
    public string Model { get; } = model;
    public string SystemInstructions { get; } = systemInstructions;
    public int MaxTokens { get; } = maxTokens;
    public string ThinkingLevel { get; } = thinkingLevel;
}
