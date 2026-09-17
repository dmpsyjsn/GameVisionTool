namespace GameVisionTool.Messages.Common;

public class LlamaResponse(string response, bool wasTruncated)
{
    public string Response { get; } = response;

    /// <summary>
    /// True when generation stopped at the token cap rather than at the model's own end of turn, so
    /// <see cref="Response"/> stops mid-sentence.
    /// </summary>
    public bool WasTruncated { get; } = wasTruncated;
}

public class ModelLoadedResponse(string modelPath)
{
    public string ModelPath { get; } = modelPath;
}
