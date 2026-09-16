using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.AgentSettings;

public sealed class GeminiAgentSettings : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public AgentGroupType GroupType { get; set; } = AgentGroupType.Backstory;
    public string Model { get; set; } = string.Empty;
    public string SystemInstructions { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 4096;
    public string ThinkingLevel { get; set; } = string.Empty;
}

public static class GeminiAgentSettingHelpers
{
    public static string[] GetCurrentSupportedModels =>
    [
        "gemini-3.8-flash",
        "gemini-3.7-flash",
        "gemini-3.1-pro-preview",
        "gemini-3.6-flash",
        "gemini-3.5-flash",
        "gemini-3.5-flash-lite"
    ];

    public static string AsOf => "2026-09-12";

    // Stored as typed here and matched case-insensitively against the SDK's own values
    // (MINIMAL/LOW/MEDIUM/HIGH) by ThinkingLevelParser. THINKING_LEVEL_UNSPECIFIED is deliberately
    // absent - leaving the level blank already means "unspecified".
    public static string[] GetCurrentThinkingLevels =>
    [
        "Minimal",
        "Low",
        "Medium",
        "High"
    ];
}