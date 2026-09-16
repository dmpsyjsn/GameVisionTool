using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Messages.Queries.Agents;

namespace GameVisionTool.Logic.Application.QueryHandlers.Agents;

public class GeminiAgentQueryHandlers(IDataStore<GeminiAgentSettings, Guid> agentStore) :
    IQueryHandler<GetGeminiAgents, GeminiAgentsViewModel>,
    IQueryHandler<GetGeminiAgent, GeminiAgentViewModel>
{
    public Result<GeminiAgentsViewModel> Handle(GetGeminiAgents query)
    {
        var itemsVm = agentStore.GetAll()
            .Select(x => new GeminiAgentViewModel(x.Id, x.Name, x.GroupType, x.Model, x.SystemInstructions, x.MaxTokens, x.ThinkingLevel))
            .ToArray();

        return Result.Ok(new GeminiAgentsViewModel(itemsVm));
    }

    public Result<GeminiAgentViewModel> Handle(GetGeminiAgent query)
    {
        var existing = agentStore.GetByIdOrDefault(query.Id);

        if (existing == null)
            return Result.Fail<GeminiAgentViewModel>("The specified agent does not exist.");

        return Result.Ok(new GeminiAgentViewModel(
            existing.Id, existing.Name, existing.GroupType, existing.Model, existing.SystemInstructions, existing.MaxTokens, existing.ThinkingLevel));
    }
}
