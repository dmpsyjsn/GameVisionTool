using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.MainSettings;

public sealed class LocalLLMSetting : Entity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string FullFilePath { get; set; } = string.Empty;

    // Defaults mirror what LlamaParametersGenerator used to hardcode, so a row saved before these
    // fields existed deserializes to the same behavior as before.
    public uint ContextSize { get; set; } = 32768;
    public int GpuLayerCount { get; set; } = -1;
}