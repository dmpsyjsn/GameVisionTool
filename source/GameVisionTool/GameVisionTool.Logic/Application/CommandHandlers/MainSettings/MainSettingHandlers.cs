using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.MainSettings;
using GameVisionTool.Messages.Commands.MainSettings;

namespace GameVisionTool.Logic.Application.CommandHandlers.MainSettings;

public class MainSettingHandlers(
    IDataStore<LocalLLMSetting, Guid> localLlmStore,
    IDataStore<ApiLlmSetting, Guid> apiLlmStore) :
    ICommandHandler<AddOrUpdateLocalLlmPath, Guid>, ICommandHandler<RemoveLocalLlmPath>,
    ICommandHandler<AddOrUpdateApiSetting>, ICommandHandler<RemoveApiSetting>
{
    private static readonly string[] ApiLlmTypeNames = Enum.GetNames<ApiLlmType>();

    public Result<Guid> Handle(AddOrUpdateLocalLlmPath command)
    {
        localLlmStore.Upsert(new LocalLLMSetting
        {
            Id = command.Id,
            Name = command.Name,
            FullFilePath = command.FullFilePath,
            ContextSize = command.ContextSize,
            GpuLayerCount = command.GpuLayerCount
        });

        return Result.Ok(command.Id);
    }

    public Result Handle(RemoveLocalLlmPath command)
    {
        var existing = localLlmStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified Local LLM path does not exist.");

        localLlmStore.Remove(existing.Id);

        return Result.Ok();
    }

    public Result Handle(AddOrUpdateApiSetting command)
    {
        // Enum.TryParse also accepts numeric strings - "12345" yields (ApiLlmType)12345, and "0"
        // yields a genuinely defined member - so neither TryParse nor Enum.IsDefined is enough on
        // its own. The type is stored as the raw string, so require an exact defined name.
        if (!ApiLlmTypeNames.Contains(command.ApiLlmType, StringComparer.Ordinal))
            return Result.Fail("The specified API LLM type is not recognized.");

        var existing = apiLlmStore.Get(x => x.Type == command.ApiLlmType).FirstOrDefault();

        if (existing != null)
        {
            existing.ApiKey = command.ApiKey;
            existing.ApiUrl = command.ApiUrl;
            apiLlmStore.Update(existing);
            return Result.Ok();
        }

        apiLlmStore.Store(new ApiLlmSetting
        {
            Id = Guid.NewGuid(),
            Type = command.ApiLlmType,
            ApiKey = command.ApiKey,
            ApiUrl = command.ApiUrl
        });

        return Result.Ok();
    }

    public Result Handle(RemoveApiSetting command)
    {
        var existing = apiLlmStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified API setting does not exist.");

        apiLlmStore.Remove(existing.Id);

        return Result.Ok();
    }
}
