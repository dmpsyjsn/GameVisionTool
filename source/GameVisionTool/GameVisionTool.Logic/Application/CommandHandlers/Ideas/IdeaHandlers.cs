using GameVisionTool.Common.Domain.Commands;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.Ideas;
using GameVisionTool.Messages.Commands.Ideas;

namespace GameVisionTool.Logic.Application.CommandHandlers.Ideas;

public class IdeaHandlers(IDataStore<Idea, Guid> ideaStore) :
    ICommandHandler<AddIdea, Guid>, ICommandHandler<UpdateIdea>, ICommandHandler<RemoveIdea>
{
    public Result<Guid> Handle(AddIdea command)
    {
        var id = Guid.NewGuid();

        ideaStore.Store(new Idea
        {
            Id = id,
            Title = command.Title
        });

        return Result.Ok(id);
    }

    public Result Handle(UpdateIdea command)
    {
        // Update replaces the whole document, so the entity has to come from the store - a fresh
        // one would null out CreatedOn.
        var existing = ideaStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified Idea does not exist.");

        existing.Title = command.Title;

        ideaStore.Update(existing);

        return Result.Ok();
    }

    public Result Handle(RemoveIdea command)
    {
        var existing = ideaStore.GetByIdOrDefault(command.Id);

        if (existing == null)
            return Result.Fail("The specified Idea does not exist.");

        ideaStore.Remove(existing.Id);

        return Result.Ok();
    }
}
