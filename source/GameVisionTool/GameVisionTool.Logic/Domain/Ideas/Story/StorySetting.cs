using System.Collections.ObjectModel;
using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Services;

namespace GameVisionTool.Logic.Domain.Ideas.Story;

// One section of an idea's story, identified by which AgentGroupType it belongs to. The same enum
// selects the agents on the generating page, so an entry and the agent that wrote it agree on group
// without a second vocabulary to keep in sync.
public sealed class StorySetting : Entity<Guid>
{
    public StorySetting() { }

    public StorySetting(Guid ideaId, AgentGroupType groupType, string mainHeading, string subHeading)
    {
        IdeaId = ideaId;
        GroupType = groupType;
        MainHeading = mainHeading;
        SubHeading = subHeading;
    }
    public Guid IdeaId { get; private set; }

    public AgentGroupType GroupType { get; private set; } = AgentGroupType.Backstory;

    public string MainHeading
    {
        get => field;
        private set => field = value ?? string.Empty;
    } = string.Empty;

    public string SubHeading
    {
        get => field;
        private set => field = value ?? string.Empty;
    } = string.Empty;
    
    public IReadOnlyList<RelatedStoryReference> RelatedStoryReferences
    {
        get => new ReadOnlyCollection<RelatedStoryReference>(_relatedStoryReferences);

        private set => _relatedStoryReferences = new List<RelatedStoryReference>(value);
    }

    private List<RelatedStoryReference> _relatedStoryReferences = new();

    // Copies rather than storing the caller's list. Holding the reference would let the caller keep
    // mutating the entity's state through the back door, straight past the IReadOnlyList the property
    // hands out - and the page that builds this list is exactly the sort of caller that reuses it.
    public void ReplaceRelatedStoryReferences(List<RelatedStoryReference>? relatedStoryReferences)
    {
        _relatedStoryReferences =
            new List<RelatedStoryReference>(relatedStoryReferences?.Where(x => x.Id != Id).Distinct() ?? []);
    }

    public IReadOnlyList<StoryEntry> Entries
    {
        get => new ReadOnlyCollection<StoryEntry>(_entries);

        private set => _entries = new List<StoryEntry>(value);
    }

    private List<StoryEntry> _entries = new();

    public void AddEntry(string title, string description)
    {
        var entry = new StoryEntry(Guid.CreateVersion7(), title, description);
        _entries.Add(entry);
    }

    public bool RemoveEntry(Guid storyEntryId)
    {
        var entryToRemove = _entries.SingleOrDefault(x => x.StoryEntryId == storyEntryId);
        if (entryToRemove != null)
        {
            _entries.Remove(entryToRemove);
            return true;
        }

        return false;
    }

    public bool InsertEntry(int position, StoryEntry storyEntry)
    {
        if (position < 0 || position > _entries.Count) return false;
        if (_entries.Contains(storyEntry)) return false;
        _entries.Insert(position, storyEntry);
        return true;
    }

    public bool MoveEntry(int newPosition, StoryEntry storyEntry)
    {
        if (newPosition < 0 || newPosition >= _entries.Count) return false;
        var currentPosition = _entries.IndexOf(storyEntry);
        if (currentPosition < 0 || currentPosition == newPosition) return false;
        if (!RemoveEntry(storyEntry.StoryEntryId)) return false;
        return InsertEntry(newPosition, storyEntry);
    }
}

public sealed class StoryEntry(Guid storyEntryId, string title, string description) : IEquatable<StoryEntry>
{
    public Guid StoryEntryId { get; } = storyEntryId;
    public string Title { get; } = title;
    public string Description { get; } = description;

    public bool Equals(StoryEntry? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return StoryEntryId.Equals(other.StoryEntryId);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((StoryEntry) obj);
    }

    public override int GetHashCode()
    {
        return StoryEntryId.GetHashCode();
    }

    public static bool operator ==(StoryEntry? left, StoryEntry? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(StoryEntry? left, StoryEntry? right)
    {
        return !Equals(left, right);
    }
}

public sealed class RelatedStoryReference(Guid id, AgentGroupType groupType) : IEquatable<RelatedStoryReference>
{
    public Guid Id { get; } = id;
    public AgentGroupType GroupType { get; } = groupType;

    public bool Equals(RelatedStoryReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is RelatedStoryReference other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(RelatedStoryReference? left, RelatedStoryReference? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RelatedStoryReference? left, RelatedStoryReference? right)
    {
        return !Equals(left, right);
    }
}
