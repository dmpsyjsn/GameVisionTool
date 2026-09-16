using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Messages.Queries.Agents;

namespace GameVisionTool.Logic.Application.QueryHandlers.Agents;

public class LlamaAgentQueryHandlers(IDataStore<LlamaAgentSettings, Guid> agentStore) :
    IQueryHandler<GetLlamaAgents, AgentsViewModel>,
    IQueryHandler<GetLlamaAgent, AgentViewModel>
{
    public Result<AgentsViewModel> Handle(GetLlamaAgents query)
    {
        var itemsVm = agentStore.GetAll()
            .Select(x => new AgentViewModel(x.Id, x.Name, x.GroupType, x.SystemPrompt, x.MaxTokens, x.Temperature, x.SuppressThinking, x.StopMarker))
            .ToArray();

        return Result.Ok(new AgentsViewModel(itemsVm));
    }

    public Result<AgentViewModel> Handle(GetLlamaAgent query)
    {
        var existing = agentStore.GetByIdOrDefault(query.Id);

        if (existing == null)
            return Result.Fail<AgentViewModel>("The specified agent does not exist.");

        return Result.Ok(new AgentViewModel(
            existing.Id, existing.Name, existing.GroupType, existing.SystemPrompt, existing.MaxTokens, existing.Temperature, existing.SuppressThinking, existing.StopMarker));
    }
}
