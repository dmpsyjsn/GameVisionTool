using GameVisionTool.Common.Domain.Queries;

namespace GameVisionTool.Messages.Queries.MainSettings;

#region Local LLM
public class GetLocalLlmFilePaths : IQuery<LocalLlmFilePathsViewModel>;

public class LocalLlmFilePathsViewModel(LocalLlmFilePathViewModel[] items)
{
    public LocalLlmFilePathViewModel[] Items { get; } = items;
}

public class LocalLlmFilePathViewModel(Guid id, string name, string fullFilePath)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string FullFilePath { get; } = fullFilePath;
}

#endregion

#region API LLM
public class GetApiLlmSettings : IQuery<ApiLlmSettingsViewModel>;

public class ApiLlmSettingsViewModel(ApiLlmSettingViewModel[] items)
{
    public ApiLlmSettingViewModel[] Items { get; } = items;
}

public class ApiLlmSettingViewModel(Guid id, string type, string apiKey, string apiUrl)
{
    public Guid Id { get; } = id;
    public string Type { get; } = type;
    public string ApiKey { get; } = apiKey;
    public string ApiUrl { get; } = apiUrl;
}
#endregion
