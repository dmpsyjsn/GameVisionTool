using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Messages.Common;

namespace GameVisionTool.Messages.Queries.Backstory.Llama;

public class GetLlamaScienceFictionBackstoryResponse(string modelPath, string content) : IAmALlamaSharpQuery<LlamaResponse>
{
    public string ModelPath { get; } = modelPath;
    public string Content { get; } = content;
}