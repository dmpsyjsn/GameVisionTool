using GameVisionTool.Common.Domain.Services;
using GameVisionTool.IntegrationTests.Helpers;
using GameVisionTool.Logic.Application.CommandHandlers.Ideas;
using GameVisionTool.Logic.Domain.Ideas;
using GameVisionTool.Messages.Commands.Ideas;
using GameVisionTool.Persistence.LiteDb;
using Xunit;

namespace GameVisionTool.IntegrationTests.Application.CommandHandlerTests.Ideas
{
    public class IdeaHandlerTests : CustomLiteDbTestDriver
    {
        private readonly IDataStore<Idea, Guid> _ideaDataStore;
        private readonly IdeaHandlers _ideaHandlers;

        public IdeaHandlerTests()
        {
            _ideaDataStore = new LiteDbDataStore<Idea, Guid>(Db);

            _ideaHandlers = new IdeaHandlers(_ideaDataStore);
        }

        #region Add

        [Fact]
        public void Test_Add_Idea_Succeeds()
        {
            var title = StringHelpers.GenerateRandomString();

            var result = _ideaHandlers.Handle(new AddIdea(title));

            Assert.True(result.IsSuccess);
            Assert.NotEqual(Guid.Empty, result.Value);

            var item = _ideaDataStore.GetByIdOrDefault(result.Value);
            Assert.NotNull(item);
            Assert.Equal(title, item.Title);
        }

        [Fact]
        public void Test_Add_Idea_Stamps_Timestamps()
        {
            var result = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            var item = _ideaDataStore.GetByIdOrDefault(result.Value);

            Assert.NotNull(item);
            Assert.NotNull(item.CreatedOn);
            Assert.NotNull(item.ModifiedOn);
        }

        [Fact]
        public void Test_Add_Idea_Returns_An_Id_That_Locates_The_Row()
        {
            // The Result<Guid> is the only way a caller learns the id the handler minted, so it has
            // to be the id the row was actually stored under.
            var result = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            Assert.True(result.IsSuccess);

            var removal = _ideaHandlers.Handle(new RemoveIdea(result.Value));

            Assert.True(removal.IsSuccess);
        }

        [Fact]
        public void Test_Add_Idea_Twice_With_The_Same_Title_Creates_Separate_Entries()
        {
            var sharedTitle = StringHelpers.GenerateRandomString();

            var first = _ideaHandlers.Handle(new AddIdea(sharedTitle));
            var second = _ideaHandlers.Handle(new AddIdea(sharedTitle));

            Assert.NotEqual(first.Value, second.Value);
            Assert.Equal(2, _ideaDataStore.GetAll().Count());
        }

        #endregion

        #region Update

        [Fact]
        public void Test_Update_Idea_Succeeds()
        {
            var updatedTitle = StringHelpers.GenerateRandomString();

            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            var result = _ideaHandlers.Handle(new UpdateIdea(added.Value, updatedTitle));

            Assert.True(result.IsSuccess);
            Assert.Single(_ideaDataStore.GetAll());

            var item = _ideaDataStore.GetByIdOrDefault(added.Value);
            Assert.NotNull(item);
            Assert.Equal(updatedTitle, item.Title);
        }

        [Fact]
        public void Test_Update_Idea_Preserves_CreatedOn()
        {
            // LiteDB replaces the whole document, so a handler that built a fresh entity instead of
            // loading the stored one would silently null this out.
            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            var createdOn = _ideaDataStore.GetById(added.Value).CreatedOn;

            _ideaHandlers.Handle(new UpdateIdea(added.Value, StringHelpers.GenerateRandomString()));

            var item = _ideaDataStore.GetById(added.Value);

            Assert.Equal(createdOn, item.CreatedOn);
        }

        [Fact]
        public void Test_Update_Idea_That_Does_Not_Exist_Fails_Without_Throwing()
        {
            var result = _ideaHandlers.Handle(new UpdateIdea(Guid.NewGuid(), StringHelpers.GenerateRandomString()));

            Assert.True(result.IsFailure);
            Assert.Equal("The specified Idea does not exist.", result.Error);
            Assert.Empty(_ideaDataStore.GetAll());
        }

        [Fact]
        public void Test_Update_Idea_Leaves_Other_Entries_Intact()
        {
            var keepTitle = StringHelpers.GenerateRandomString();

            var keep = _ideaHandlers.Handle(new AddIdea(keepTitle));
            var change = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            _ideaHandlers.Handle(new UpdateIdea(change.Value, StringHelpers.GenerateRandomString()));

            var untouched = _ideaDataStore.GetByIdOrDefault(keep.Value);
            Assert.NotNull(untouched);
            Assert.Equal(keepTitle, untouched.Title);
        }

        #endregion

        #region Remove

        [Fact]
        public void Test_Remove_Idea_Succeeds()
        {
            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            var result = _ideaHandlers.Handle(new RemoveIdea(added.Value));

            Assert.True(result.IsSuccess);
            Assert.Null(_ideaDataStore.GetByIdOrDefault(added.Value));
            Assert.Empty(_ideaDataStore.GetAll());
        }

        [Fact]
        public void Test_Remove_Idea_That_Does_Not_Exist_Fails_Without_Throwing()
        {
            var result = _ideaHandlers.Handle(new RemoveIdea(Guid.NewGuid()));

            Assert.True(result.IsFailure);
            Assert.Equal("The specified Idea does not exist.", result.Error);
        }

        [Fact]
        public void Test_Remove_Idea_Twice_Fails_The_Second_Time()
        {
            // IDataStore.Remove throws when the row is gone, so the handler has to check first -
            // deletes are deliberately not idempotent.
            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            Assert.True(_ideaHandlers.Handle(new RemoveIdea(added.Value)).IsSuccess);

            var second = _ideaHandlers.Handle(new RemoveIdea(added.Value));

            Assert.True(second.IsFailure);
            Assert.Equal("The specified Idea does not exist.", second.Error);
        }

        [Fact]
        public void Test_Remove_Idea_Leaves_Other_Entries_Intact()
        {
            var keep = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));
            var remove = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            _ideaHandlers.Handle(new RemoveIdea(remove.Value));

            Assert.Single(_ideaDataStore.GetAll());
            Assert.NotNull(_ideaDataStore.GetByIdOrDefault(keep.Value));
        }

        #endregion
    }
}
