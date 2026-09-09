using LLama;
using LLama.Common;

namespace GameVisionTool.Integration.LlamaSharp;

public sealed class LlamaModelGenerator(ModelParams parameters) : IDisposable
{
    private readonly LLamaWeights _model = LLamaWeights.LoadFromFile(parameters);

    public LLamaWeights Model => _model;

    public void Dispose() => _model.Dispose();
}