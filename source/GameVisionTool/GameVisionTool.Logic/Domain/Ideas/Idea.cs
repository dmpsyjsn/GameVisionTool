using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.Ideas;

public class Idea : Entity<Guid>
{
    public string Title { get; set; } = string.Empty;
}
