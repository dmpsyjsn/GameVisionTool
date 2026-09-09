using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.Ideas.Backstory;

public class BackstorySetting(Guid ideaId, string chapter, string title, string description) : Entity<Guid>
{
    public string Chapter { get; } = chapter;
    public string Title { get; } = title;
    public string Description { get; } = description;
}