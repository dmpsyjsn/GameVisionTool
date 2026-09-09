using LLama;

namespace GameVisionTool.Integration.LlamaSharp;

public sealed class LlamaModelProvider : IDisposable
{
    private readonly Lock _gate = new();
    private string? _loadedPath;
    private LlamaModelGenerator? _current;

    public LLamaWeights GetOrLoad(string modelPath)
    {
        LlamaSharpInitializer.Initialize();   // before any llama.cpp call, always

        lock (_gate)
        {
            if (_current is not null && _loadedPath == modelPath)
                return _current.Model;

            _current?.Dispose();
            _current = new LlamaModelGenerator(new LlamaParametersGenerator(modelPath).Parameters);
            _loadedPath = modelPath;
            return _current.Model;
        }
    }

    public void Dispose() { lock (_gate) { _current?.Dispose(); _current = null; } }
}
