using GameVisionTool.Common.Domain.Queries;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.Domain.Ideas;
using GameVisionTool.Messages.Queries.Ideas;

namespace GameVisionTool.Logic.Application.QueryHandlers.Ideas;

public class IdeaQueryHandlers(IDataStore<Idea, Guid> ideaStore) :
    IQueryHandler<GetIdeas, IdeasViewModel>,
    IQueryHandler<GetIdea, IdeaViewModel>
{
    public Result<IdeasViewModel> Handle(GetIdeas query)
    {
        var itemsVm = ideaStore.GetAll()
            .Select(x => new IdeaViewModel(x.Id, x.Title))
            .ToArray();

        var vm = new IdeasViewModel(itemsVm);

        return Result.Ok(vm);
    }

    public Result<IdeaViewModel> Handle(GetIdea query)
    {
        var existing = ideaStore.GetByIdOrDefault(query.Id);

        if (existing == null)
            return Result.Fail<IdeaViewModel>("The specified Idea does not exist.");

        return Result.Ok(new IdeaViewModel(existing.Id, existing.Title));
    }
}
