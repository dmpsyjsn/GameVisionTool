using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Common;

namespace GameVisionTool.Messages.Queries.Llama;

public class LoadLlamaModel(string modelPath, uint contextSize, int gpuLayerCount) : IAmALlamaSharpQuery<ModelLoadedResponse>
{
    public string ModelPath { get; } = modelPath;
    public uint ContextSize { get; } = contextSize;
    public int GpuLayerCount { get; } = gpuLayerCount;
}
