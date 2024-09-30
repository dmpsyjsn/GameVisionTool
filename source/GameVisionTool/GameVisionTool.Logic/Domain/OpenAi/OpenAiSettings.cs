using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.OpenAi;

public class OpenAiSettings : Entity
{
    public const int DefaultId = 1;
    public string ApiKey { get; set; } = string.Empty;
}