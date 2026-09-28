using LLama;

namespace GameVisionTool.Integration.LlamaSharp;

public sealed class LlamaModelProvider : ILlamaModelProvider, IDisposable
{
    private readonly Lock _gate = new();
    private (string ModelPath, uint ContextSize, int GpuLayerCount)? _loaded;
    private LlamaModelGenerator? _current;

    /// <summary>
    /// Loads the weights without handing them back, so the caller pays the load now instead of on its
    /// first generation. Off the calling thread because loading is a long blocking native call.
    /// </summary>
    public Task EnsureLoaded(string modelPath, uint contextSize, int gpuLayerCount) =>
        Task.Run(() => GetOrLoad(modelPath, contextSize, gpuLayerCount));

    public LLamaWeights GetOrLoad(string modelPath, uint contextSize, int gpuLayerCount)
    {
        LlamaSharpInitializer.Initialize();   // before any llama.cpp call, always

        lock (_gate)
        {
            var requested = (modelPath, contextSize, gpuLayerCount);

            if (_current is not null && _loaded == requested)
                return _current.Model;

            _current?.Dispose();
            _current = new LlamaModelGenerator(new LlamaParametersGenerator(modelPath, gpuLayerCount, contextSize).Parameters);
            _loaded = requested;
            return _current.Model;
        }
    }

    public void Dispose() { lock (_gate) { _current?.Dispose(); _current = null; } }
}
