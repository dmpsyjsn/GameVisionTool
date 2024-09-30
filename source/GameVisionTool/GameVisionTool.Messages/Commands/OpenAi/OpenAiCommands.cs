using GameVisionTool.Common.Domain.Commands;

namespace GameVisionTool.Messages.Commands.OpenAi;

public class UpsertOpenAiSettings(string apiKey) : ICommand
{
    public string ApiKey { get; } = apiKey;
}