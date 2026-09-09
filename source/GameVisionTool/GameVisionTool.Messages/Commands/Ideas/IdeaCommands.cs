using GameVisionTool.Common.Domain.Commands;

namespace GameVisionTool.Messages.Commands.Ideas;

public class AddIdea(string title) : ICommand<Guid>
{
    public string Title { get; } = title;
}

public class UpdateIdea(Guid id, string title) : ICommand
{
    public Guid Id { get; } = id;
    public string Title { get; } = title;
}

public class RemoveIdea(Guid id) : ICommand
{
    public Guid Id { get; } = id;
}
