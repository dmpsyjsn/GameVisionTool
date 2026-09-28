using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.IntegrationTests.Helpers;
using GameVisionTool.Logic.Domain.Ideas.Story;
using GameVisionTool.Persistence.LiteDb;
using Xunit;

namespace GameVisionTool.IntegrationTests.Domain
{
    /// <summary>
    /// StorySetting has no command or handler yet, so these go straight at the store. What is being
    /// checked is the mapping, not any logic: both collections on the entity are exposed as
    /// IReadOnlyList over a private backing list with a private setter, and RelatedStoryReference has
    /// get-only properties reachable only through a parameterised constructor. Those are exactly the
    /// shapes a document mapper can write out and then fail to read back - silently, as an empty list
    /// on an otherwise intact document. Nothing would notice until a generation went out with no
    /// context attached.
    ///
    /// The relation rules live here too rather than in a separate file: they are enforced on the way
    /// into the same list these tests read back, and dedup in particular depends on
    /// RelatedStoryReference's equality contract holding.
    /// </summary>
    public class StorySettingPersistenceTests : CustomLiteDbTestDriver
    {
        private readonly IDataStore<StorySetting, Guid> _store;

        public StorySettingPersistenceTests()
        {
            _store = new LiteDbDataStore<StorySetting, Guid>(Db);
        }

        private static StorySetting NewSetting(
            Guid ideaId,
            AgentGroupType groupType = AgentGroupType.Character,
            List<RelatedStoryReference>? related = null)
        {
            var setting = new StorySetting(ideaId, groupType, StringHelpers.GenerateRandomString(),
                StringHelpers.GenerateRandomString())
            {
                Id = Guid.CreateVersion7()
            };

            if (related != null)
            {
                setting.ReplaceRelatedStoryReferences(related);
            }

            return setting;
        }

        #region Round-tripping

        [Fact]
        public void Test_Scalar_Fields_Round_Trip()
        {
            var ideaId = Guid.CreateVersion7();
            var setting = NewSetting(ideaId, AgentGroupType.Quest);

            _store.Store(setting);

            var stored = _store.GetById(setting.Id);

            Assert.Equal(ideaId, stored.IdeaId);
            Assert.Equal(AgentGroupType.Quest, stored.GroupType);
            Assert.Equal(setting.MainHeading, stored.MainHeading);
            Assert.Equal(setting.SubHeading, stored.SubHeading);
        }

        [Fact]
        public void Test_Related_Story_References_Round_Trip()
        {
            // The whole point of the relation list is to be read back on a later run and turned into
            // prompt context. A reference that deserializes as an empty list, or with Guid.Empty and
            // default(AgentGroupType), produces a generation with no context and no error.
            var backstoryId = Guid.CreateVersion7();
            var worldId = Guid.CreateVersion7();

            var setting = NewSetting(Guid.CreateVersion7(), AgentGroupType.Character, related:
            [
                new RelatedStoryReference(backstoryId, AgentGroupType.Backstory),
                new RelatedStoryReference(worldId, AgentGroupType.World)
            ]);

            _store.Store(setting);

            var stored = _store.GetById(setting.Id);

            Assert.Equal(2, stored.RelatedStoryReferences.Count);

            // Asserted member by member rather than with Contains: RelatedStoryReference.Equals compares
            // Id alone, so a Contains check would pass even if GroupType came back as the default.
            var backstory = stored.RelatedStoryReferences.Single(x => x.Id == backstoryId);
            Assert.Equal(AgentGroupType.Backstory, backstory.GroupType);

            var world = stored.RelatedStoryReferences.Single(x => x.Id == worldId);
            Assert.Equal(AgentGroupType.World, world.GroupType);
        }

        [Fact]
        public void Test_Entries_Round_Trip()
        {
            var setting = NewSetting(Guid.CreateVersion7());
            setting.AddEntry("A smuggler with a debt.", "Owes the wrong people.");
            setting.AddEntry("A freighter childhood.", "Never saw a planet until twelve.");

            var expectedIds = setting.Entries.Select(x => x.StoryEntryId).ToList();

            _store.Store(setting);

            var stored = _store.GetById(setting.Id);

            Assert.Equal(2, stored.Entries.Count);
            Assert.Equal(expectedIds, stored.Entries.Select(x => x.StoryEntryId).ToList());
            Assert.Equal("A smuggler with a debt.", stored.Entries[0].Title);
            Assert.Equal("Owes the wrong people.", stored.Entries[0].Description);
        }

        [Fact]
        public void Test_Entry_Order_Round_Trips()
        {
            // Order is meaningful - InsertEntry and MoveEntry exist to control it, and it is the order
            // the entries will be laid into the prompt.
            var setting = NewSetting(Guid.CreateVersion7());
            setting.AddEntry("first", "1");
            setting.AddEntry("second", "2");
            setting.AddEntry("third", "3");

            setting.MoveEntry(0, setting.Entries[2]);

            var expectedOrder = setting.Entries.Select(x => x.Title).ToList();
            Assert.Equal(["third", "first", "second"], expectedOrder);

            _store.Store(setting);

            var stored = _store.GetById(setting.Id);

            Assert.Equal(expectedOrder, stored.Entries.Select(x => x.Title).ToList());
        }

        [Fact]
        public void Test_Update_Preserves_Both_Collections()
        {
            // LiteDB replaces the whole document on update. Both collections have private setters, so
            // if the mapper cannot write through them the update silently empties them.
            var setting = NewSetting(Guid.CreateVersion7(), AgentGroupType.Character, related:
            [
                new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.Backstory)
            ]);
            setting.AddEntry("kept", "should survive an unrelated update");

            _store.Store(setting);

            var loaded = _store.GetById(setting.Id);
            _store.Update(loaded);

            var reloaded = _store.GetById(setting.Id);

            Assert.Single(reloaded.RelatedStoryReferences);
            Assert.Single(reloaded.Entries);
            Assert.Equal("kept", reloaded.Entries[0].Title);
        }

        [Fact]
        public void Test_Settings_Are_Queryable_By_Idea_And_Group()
        {
            // How the generating page will find a setting's related rows: LiteDB translates only a
            // subset of LINQ, so the predicate the lookup will actually use is worth pinning down
            // before a handler is written against it.
            var ideaId = Guid.CreateVersion7();
            var otherIdeaId = Guid.CreateVersion7();

            _store.Store(NewSetting(ideaId, AgentGroupType.Backstory));
            _store.Store(NewSetting(ideaId, AgentGroupType.World));
            _store.Store(NewSetting(otherIdeaId, AgentGroupType.Backstory));

            var forIdea = _store.Get(x => x.IdeaId == ideaId).ToList();
            Assert.Equal(2, forIdea.Count);

            var backstoryForIdea = _store.Get(x => x.IdeaId == ideaId && x.GroupType == AgentGroupType.Backstory).ToList();
            Assert.Single(backstoryForIdea);
            Assert.Equal(AgentGroupType.Backstory, backstoryForIdea[0].GroupType);
        }

        #endregion

        #region Relation rules

        [Fact]
        public void Test_Replacing_Relations_Copies_The_Callers_List()
        {
            // The page that builds this list is the sort of caller that reuses and clears it between
            // selections. Holding the reference would let those edits reach through the IReadOnlyList
            // the property hands out, and would put an un-stored change into a loaded entity.
            var keptId = Guid.CreateVersion7();
            var caller = new List<RelatedStoryReference>
            {
                new(keptId, AgentGroupType.Backstory)
            };

            var setting = NewSetting(Guid.CreateVersion7());
            setting.ReplaceRelatedStoryReferences(caller);

            caller.Clear();
            caller.Add(new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.Item));

            Assert.Single(setting.RelatedStoryReferences);
            Assert.Equal(keptId, setting.RelatedStoryReferences[0].Id);
            Assert.Equal(AgentGroupType.Backstory, setting.RelatedStoryReferences[0].GroupType);
        }

        [Fact]
        public void Test_Replacing_Relations_Overwrites_Rather_Than_Appends()
        {
            var setting = NewSetting(Guid.CreateVersion7(), related:
            [
                new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.Backstory),
                new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.World)
            ]);

            var keptId = Guid.CreateVersion7();
            setting.ReplaceRelatedStoryReferences([new RelatedStoryReference(keptId, AgentGroupType.Quest)]);

            Assert.Single(setting.RelatedStoryReferences);
            Assert.Equal(keptId, setting.RelatedStoryReferences[0].Id);
        }

        [Fact]
        public void Test_Replacing_Relations_With_An_Empty_List_Clears_Them()
        {
            // Unticking every box in the UI has to actually clear the set. A no-op on empty would keep
            // the old relations and write them straight back on the next save.
            var setting = NewSetting(Guid.CreateVersion7(), related:
            [
                new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.Backstory)
            ]);

            Assert.Single(setting.RelatedStoryReferences);

            setting.ReplaceRelatedStoryReferences([]);

            Assert.Empty(setting.RelatedStoryReferences);
        }

        [Fact]
        public void Test_Cleared_Relations_Survive_A_Round_Trip()
        {
            var setting = NewSetting(Guid.CreateVersion7(), related:
            [
                new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.Backstory)
            ]);

            _store.Store(setting);

            var loaded = _store.GetById(setting.Id);
            loaded.ReplaceRelatedStoryReferences([]);
            _store.Update(loaded);

            Assert.Empty(_store.GetById(setting.Id).RelatedStoryReferences);
        }

        [Fact]
        public void Test_Duplicate_References_Are_Collapsed()
        {
            var duplicatedId = Guid.CreateVersion7();

            var setting = NewSetting(Guid.CreateVersion7(), related:
            [
                new RelatedStoryReference(duplicatedId, AgentGroupType.Backstory),
                new RelatedStoryReference(duplicatedId, AgentGroupType.Backstory)
            ]);

            Assert.Single(setting.RelatedStoryReferences);
            Assert.Equal(duplicatedId, setting.RelatedStoryReferences[0].Id);
        }

        [Fact]
        public void Test_The_Same_Setting_Under_Two_Groups_Is_Still_A_Duplicate()
        {
            // RelatedStoryReference.Equals compares Id alone, so the group does not make a second
            // reference distinct. That is the intended reading - GroupType is denormalized from the
            // target, so two references to one setting disagreeing on group is a contradiction, not
            // two relationships. Distinct() only honours this because GetHashCode is Id-only too.
            var settingId = Guid.CreateVersion7();

            var setting = NewSetting(Guid.CreateVersion7(), related:
            [
                new RelatedStoryReference(settingId, AgentGroupType.Backstory),
                new RelatedStoryReference(settingId, AgentGroupType.World)
            ]);

            Assert.Single(setting.RelatedStoryReferences);
        }

        [Fact]
        public void Test_A_Setting_Cannot_Relate_To_Itself()
        {
            var setting = NewSetting(Guid.CreateVersion7());

            setting.ReplaceRelatedStoryReferences(
            [
                new RelatedStoryReference(setting.Id, AgentGroupType.Character),
                new RelatedStoryReference(Guid.CreateVersion7(), AgentGroupType.World)
            ]);

            Assert.Single(setting.RelatedStoryReferences);
            Assert.DoesNotContain(setting.RelatedStoryReferences, x => x.Id == setting.Id);
        }

        [Fact]
        public void Test_Relation_Order_Is_Preserved()
        {
            // Order decides the order the context is laid into the prompt, and Distinct keeps first
            // occurrence, so the filtering does not quietly reshuffle the user's selection.
            var first = Guid.CreateVersion7();
            var second = Guid.CreateVersion7();
            var third = Guid.CreateVersion7();

            var setting = NewSetting(Guid.CreateVersion7(), related:
            [
                new RelatedStoryReference(first, AgentGroupType.Backstory),
                new RelatedStoryReference(second, AgentGroupType.World),
                new RelatedStoryReference(first, AgentGroupType.Backstory),
                new RelatedStoryReference(third, AgentGroupType.Quest)
            ]);

            _store.Store(setting);

            var stored = _store.GetById(setting.Id);

            Assert.Equal([first, second, third], stored.RelatedStoryReferences.Select(x => x.Id).ToList());
        }

        #endregion
    }
}
