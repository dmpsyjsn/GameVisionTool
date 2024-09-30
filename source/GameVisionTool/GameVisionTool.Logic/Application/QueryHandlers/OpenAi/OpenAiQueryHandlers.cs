using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.OpenAi;
using GameVisionTool.Messages.Queries.OpenAi;

namespace GameVisionTool.Logic.Application.QueryHandlers.OpenAi;

public class OpenAiQueryHandlers(IDataStore<OpenAiSettings> openAiSettingsDataStore) : IAsyncQueryHandler<GetOpenAiSettings, OpenAiSettingsViewModel>
{
    public async Task<Result<OpenAiSettingsViewModel>> HandleAsync(GetOpenAiSettings query)
    {
        var openAiSettings = await openAiSettingsDataStore.GetSingleOrDefault();

        return Result.Ok(new OpenAiSettingsViewModel(openAiSettings?.ApiKey ?? string.Empty));
    }
}