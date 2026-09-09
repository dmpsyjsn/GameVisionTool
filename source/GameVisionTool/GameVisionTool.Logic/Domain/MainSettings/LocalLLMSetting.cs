using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.MainSettings;

public sealed class LocalLLMSetting : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string FullFilePath { get; set; } = string.Empty;
}