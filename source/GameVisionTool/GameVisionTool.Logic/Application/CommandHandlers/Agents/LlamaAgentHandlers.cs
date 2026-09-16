using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Messages.Commands.Agents;

namespace GameVisionTool.Logic.Application.CommandHandlers.Agents;

public class LlamaAgentHandlers(IDataStore<LlamaAgentSettings, Guid> agentStore) :
    ICommandHandler<AddLlamaAgent, Guid>, ICommandHandler<UpdateLlamaAgent>, ICommandHandler<RemoveLlamaAgent>
{
    public Result<Guid> Handle(AddLlamaAgent command)
    {
        var id = Guid.NewGuid();

        agentStore.Store(new LlamaAgentSettings
        {
            Id = id,
            Name = command.Name,
            GroupType = command.GroupType,
            SystemPrompt = command.SystemPrompt,
            MaxTokens = command.MaxTokens,
            Temperature = command.Temperature,
            SuppressThinking = command.SuppressThinking,
            StopMarker = command.StopMarker
        });

        return Result.Ok(id);
    }

    public Result Handle(UpdateLlamaAgent command)
    {
        // Update replaces the whole document, so the entity has to come from the store - a fresh
        // one would null out CreatedOn.
        var existing = agentStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified agent does not exist.");

        existing.Name = command.Name;
        existing.GroupType = command.GroupType;
        existing.SystemPrompt = command.SystemPrompt;
        existing.MaxTokens = command.MaxTokens;
        existing.Temperature = command.Temperature;
        existing.SuppressThinking = command.SuppressThinking;
        existing.StopMarker = command.StopMarker;

        agentStore.Update(existing);

        return Result.Ok();
    }

    public Result Handle(RemoveLlamaAgent command)
    {
        var existing = agentStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified agent does not exist.");

        agentStore.Remove(existing.Id);

        return Result.Ok();
    }
}
