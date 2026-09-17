using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Messages.Commands.Agents;

namespace GameVisionTool.Logic.Application.CommandHandlers.Agents;

public class GeminiAgentHandlers(IDataStore<GeminiAgentSettings, Guid> agentStore) :
    ICommandHandler<AddGeminiAgent, Guid>, ICommandHandler<UpdateGeminiAgent>, ICommandHandler<RemoveGeminiAgent>
{
    public Result<Guid> Handle(AddGeminiAgent command)
    {
        var id = Guid.NewGuid();

        agentStore.Store(new GeminiAgentSettings
        {
            Id = id,
            Name = command.Name,
            GroupType = command.GroupType,
            Model = command.Model,
            SystemInstructions = command.SystemInstructions,
            MaxTokens = command.MaxTokens,
            ThinkingLevel = command.ThinkingLevel
        });

        return Result.Ok(id);
    }

    public Result Handle(UpdateGeminiAgent command)
    {
        // Update replaces the whole document, so the entity has to come from the store - a fresh
        // one would null out CreatedOn.
        var existing = agentStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified agent does not exist.");

        existing.Name = command.Name;
        existing.GroupType = command.GroupType;
        existing.Model = command.Model;
        existing.SystemInstructions = command.SystemInstructions;
        existing.MaxTokens = command.MaxTokens;
        existing.ThinkingLevel = command.ThinkingLevel;

        agentStore.Update(existing);

        return Result.Ok();
    }

    public Result Handle(RemoveGeminiAgent command)
    {
        var existing = agentStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified agent does not exist.");

        agentStore.Remove(existing.Id);

        return Result.Ok();
    }
}
