using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Commands;

namespace GameVisionTool.Messages.Commands.Agents;

public class AddGeminiAgent(string name, AgentGroupType groupType, string model, string systemInstructions, int maxTokens = 4096, string thinkingLevel = "") : ICommand<Guid>
{
    public string Name { get; } = name;
    public AgentGroupType GroupType { get; } = groupType;
    public string Model { get; } = model;
    public string SystemInstructions { get; } = systemInstructions;
    public int MaxTokens { get; } = maxTokens;
    public string ThinkingLevel { get; } = thinkingLevel;
}

public class UpdateGeminiAgent(Guid id, string name, AgentGroupType groupType, string model, string systemInstructions, int maxTokens = 4096, string thinkingLevel = "") : ICommand
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public AgentGroupType GroupType { get; } = groupType;
    public string Model { get; } = model;
    public string SystemInstructions { get; } = systemInstructions;
    public int MaxTokens { get; } = maxTokens;
    public string ThinkingLevel { get; } = thinkingLevel;
}

public class RemoveGeminiAgent(Guid id) : ICommand
{
    public Guid Id { get; } = id;
}
