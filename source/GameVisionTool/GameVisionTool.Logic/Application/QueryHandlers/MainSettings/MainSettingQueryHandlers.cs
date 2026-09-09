using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.MainSettings;
using GameVisionTool.Messages.Queries.MainSettings;

namespace GameVisionTool.Logic.Application.QueryHandlers.MainSettings;

public class MainSettingQueryHandlers(
    IDataStore<LocalLLMSetting, Guid> localLlmStore,
    IDataStore<ApiLlmSetting, Guid> apiLlmStore) :
    IQueryHandler<GetLocalLlmFilePaths, LocalLlmFilePathsViewModel>,
    IQueryHandler<GetApiLlmSettings, ApiLlmSettingsViewModel>
{
    public Result<LocalLlmFilePathsViewModel> Handle(GetLocalLlmFilePaths query)
    {
        var itemsVm = localLlmStore.GetAll()
            .Select(x => new LocalLlmFilePathViewModel(x.Id, x.Name, x.FullFilePath))
            .ToArray();

        var vm = new LocalLlmFilePathsViewModel(itemsVm);

        return Result.Ok(vm);
    }

    public Result<ApiLlmSettingsViewModel> Handle(GetApiLlmSettings query)
    {
        var itemsVm = apiLlmStore.GetAll()
            .Select(x => new ApiLlmSettingViewModel(x.Id, x.Type, x.ApiKey, x.ApiUrl))
            .ToArray();

        var vm = new ApiLlmSettingsViewModel(itemsVm);

        return Result.Ok(vm);
    }
}
