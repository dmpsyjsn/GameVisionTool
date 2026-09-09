using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Common;

namespace GameVisionTool.Messages.Queries.Llama;

public class LoadLlamaModel(string modelPath) : IAmALlamaSharpQuery<ModelLoadedResponse>
{
    public string ModelPath { get; } = modelPath;

}
