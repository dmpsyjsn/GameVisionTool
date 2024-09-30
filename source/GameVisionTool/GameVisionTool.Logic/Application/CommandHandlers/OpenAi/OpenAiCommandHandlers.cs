using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.OpenAi;
using GameVisionTool.Messages.Commands.OpenAi;

namespace GameVisionTool.Logic.Application.CommandHandlers.OpenAi;

public class OpenAiCommandHandlers(IDataStore<OpenAiSettings> openAiDataStore) : IAsyncCommandHandler<UpsertOpenAiSettings>
{
    public async Task<Result> HandleAsync(UpsertOpenAiSettings command)
    {
        var entity = new OpenAiSettings
        {
            Id = OpenAiSettings.DefaultId,
            ApiKey = command.ApiKey
        };
        await openAiDataStore.Upsert(entity);

        return Result.Ok();
    }
}