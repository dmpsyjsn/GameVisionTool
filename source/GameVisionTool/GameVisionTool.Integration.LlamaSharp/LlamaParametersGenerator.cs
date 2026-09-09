using LLama.Common;

namespace GameVisionTool.Integration.LlamaSharp;

// ContextSize is set rather than left null. Null means "use the model's trained context", which on
// a modern long-context .gguf can be 128k - a KV cache far larger than this app needs, allocated up
// front in VRAM.
//
// 32768 is sized for a ~12B model at a 4- or 5-bit quant on a 16GB card: roughly 160KB of KV cache
// per token, so ~5GB here, on top of ~7.5GB of weights. An 8-bit quant of the same model does not
// leave room for this and needs the context dropped to 8192.
public sealed class LlamaParametersGenerator(string modelPath, int gpuLayerCount = -1, uint contextSize = 32768)
{
    public ModelParams Parameters { get; } = new(modelPath)
    {
        GpuLayerCount = gpuLayerCount,
        ContextSize = contextSize,
    };
}