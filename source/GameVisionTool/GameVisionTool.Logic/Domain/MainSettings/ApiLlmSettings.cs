using System.ComponentModel;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.MainSettings;

public sealed class ApiLlmSetting : Entity<Guid>
{
    public string Type { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
}

public enum ApiLlmType
{
    [Description("Google Gemini")]
    GoogleGemini = 0,
}