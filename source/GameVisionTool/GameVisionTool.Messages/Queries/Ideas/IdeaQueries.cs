using GameVisionTool.Common.Domain.Queries;

namespace GameVisionTool.Messages.Queries.Ideas;

public class GetIdeas : IQuery<IdeasViewModel>;

public class GetIdea(Guid id) : IQuery<IdeaViewModel>
{
    public Guid Id { get; } = id;
}

public class IdeasViewModel(IdeaViewModel[] items)
{
    public IdeaViewModel[] Items { get; } = items;
}

public class IdeaViewModel(Guid id, string title)
{
    public Guid Id { get; } = id;
    public string Title { get; } = title;
}
