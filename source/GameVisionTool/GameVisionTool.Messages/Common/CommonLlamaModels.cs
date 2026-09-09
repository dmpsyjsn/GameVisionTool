namespace GameVisionTool.Messages.Common;

public class LlamaResponse(string response)
{
    public string Response { get; } = response;
}

public class ModelLoadedResponse(string modelPath)
{
    public string ModelPath { get; } = modelPath;
}
