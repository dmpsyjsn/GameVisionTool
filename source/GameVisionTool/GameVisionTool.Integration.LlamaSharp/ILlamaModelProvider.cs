namespace GameVisionTool.Integration.LlamaSharp;

/// <summary>
/// Warming the weights, for the page that wants the first generation to be fast rather than wanting a
/// response. Separate from <c>ILlamaAgent</c> because it is a different job with a different caller -
/// the model picker warms on selection, the Generate button generates.
///
/// Returns nothing rather than the loaded <c>LLamaWeights</c>. The handle is the integration's own
/// state and a caller outside it has no use for one, so keeping it off the interface is what stops the
/// LlamaSharp API leaking into <c>Logic</c> through the warm-up path.
/// </summary>
public interface ILlamaModelProvider
{
    Task EnsureLoaded(string modelPath, uint contextSize, int gpuLayerCount);
}
