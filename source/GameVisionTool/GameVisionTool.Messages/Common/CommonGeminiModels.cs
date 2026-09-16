namespace GameVisionTool.Messages.Common;

public class GeminiResponse(string response)
{
    public string Response { get; } = response;
}
