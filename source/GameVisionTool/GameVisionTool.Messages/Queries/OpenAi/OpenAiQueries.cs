using GameVisionTool.Common.Domain.Queries;

namespace GameVisionTool.Messages.Queries.OpenAi;

#region Queries

public class GetOpenAiSettings : IQuery<OpenAiSettingsViewModel>;

#endregion


#region View Models

public class OpenAiSettingsViewModel(string apiKey)
{
    public string ApiKey { get; } = apiKey;
}

#endregion