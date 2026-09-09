using GameVisionTool.Common.Domain.Commands;

namespace GameVisionTool.Messages.Commands.MainSettings;

public class AddOrUpdateLocalLlmPath(Guid id, string name, string fullFilePath) : ICommand<Guid>
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string FullFilePath { get; } = fullFilePath; // Should include the file name and extension
}

public class RemoveLocalLlmPath(Guid id) : ICommand
{
    public Guid Id { get; } = id;
}

public class AddOrUpdateApiSetting(string apiLlmType, string apiKey, string apiUrl) : ICommand
{
    public string ApiLlmType { get; } = apiLlmType;
    public string ApiKey { get; } = apiKey;
    public string ApiUrl { get; } = apiUrl;
}

public class RemoveApiSetting(Guid id) : ICommand
{
    public Guid Id { get; } = id;
}