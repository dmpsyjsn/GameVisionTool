using GameVisionTool.Common.Domain.Services;
using GameVisionTool.IntegrationTests.Helpers;
using GameVisionTool.Logic.Application.CommandHandlers.Ideas;
using GameVisionTool.Logic.Application.QueryHandlers.Ideas;
using GameVisionTool.Logic.Domain.Ideas;
using GameVisionTool.Messages.Commands.Ideas;
using GameVisionTool.Messages.Queries.Ideas;
using GameVisionTool.Persistence.LiteDb;
using Xunit;

namespace GameVisionTool.IntegrationTests.Application.QueryHandlerTests.Ideas
{
    public class IdeaQueryHandlerTests : CustomLiteDbTestDriver
    {
        private readonly IDataStore<Idea, Guid> _ideaDataStore;
        private readonly IdeaHandlers _ideaHandlers;
        private readonly IdeaQueryHandlers _ideaQueryHandlers;

        public IdeaQueryHandlerTests()
        {
            _ideaDataStore = new LiteDbDataStore<Idea, Guid>(Db);

            // The command handlers are the only way rows get written, so the queries are read back
            // against exactly what the write side produces.
            _ideaHandlers = new IdeaHandlers(_ideaDataStore);
            _ideaQueryHandlers = new IdeaQueryHandlers(_ideaDataStore);
        }

        #region GetIdeas

        [Fact]
        public void Test_Get_Ideas_Returns_Every_Stored_Idea()
        {
            var firstTitle = StringHelpers.GenerateRandomString();
            var secondTitle = StringHelpers.GenerateRandomString();

            _ideaHandlers.Handle(new AddIdea(firstTitle));
            _ideaHandlers.Handle(new AddIdea(secondTitle));

            var result = _ideaQueryHandlers.Handle(new GetIdeas());

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value.Items.Length);
            Assert.Contains(result.Value.Items, x => x.Title == firstTitle);
            Assert.Contains(result.Value.Items, x => x.Title == secondTitle);
        }

        [Fact]
        public void Test_Get_Ideas_With_No_Ideas_Succeeds_With_An_Empty_Collection()
        {
            // An empty store is not a failure - Result.Ok with no items is what the UI binds to.
            var result = _ideaQueryHandlers.Handle(new GetIdeas());

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Items);
        }

        [Fact]
        public void Test_Get_Ideas_Carries_The_Id_Needed_To_Update_Or_Remove()
        {
            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            var result = _ideaQueryHandlers.Handle(new GetIdeas());

            var item = Assert.Single(result.Value.Items);
            Assert.Equal(added.Value, item.Id);
        }

        [Fact]
        public void Test_Get_Ideas_Reflects_An_Update()
        {
            var updatedTitle = StringHelpers.GenerateRandomString();

            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));
            _ideaHandlers.Handle(new UpdateIdea(added.Value, updatedTitle));

            var result = _ideaQueryHandlers.Handle(new GetIdeas());

            var item = Assert.Single(result.Value.Items);
            Assert.Equal(updatedTitle, item.Title);
        }

        [Fact]
        public void Test_Get_Ideas_Reflects_A_Removal()
        {
            var added = _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));
            _ideaHandlers.Handle(new RemoveIdea(added.Value));

            var result = _ideaQueryHandlers.Handle(new GetIdeas());

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value.Items);
        }

        #endregion

        #region GetIdea

        [Fact]
        public void Test_Get_Idea_Returns_The_Requested_Idea()
        {
            var title = StringHelpers.GenerateRandomString();

            var added = _ideaHandlers.Handle(new AddIdea(title));
            _ideaHandlers.Handle(new AddIdea(StringHelpers.GenerateRandomString()));

            var result = _ideaQueryHandlers.Handle(new GetIdea(added.Value));

            Assert.True(result.IsSuccess);
            Assert.Equal(added.Value, result.Value.Id);
            Assert.Equal(title, result.Value.Title);
        }

        [Fact]
        public void Test_Get_Idea_That_Does_Not_Exist_Fails_Without_Throwing()
        {
            var result = _ideaQueryHandlers.Handle(new GetIdea(Guid.NewGuid()));

            Assert.True(result.IsFailure);
            Assert.Equal("The specified Idea does not exist.", result.Error);
        }

        [Fact]
        public void Test_Get_Idea_On_A_Failure_Returns_A_Default_Value_Rather_Than_Throwing()
        {
            // Result<T>.Value hands back default(T) on failure by design, so a caller that ignores
            // IsFailure gets null here instead of an exception from the Value getter.
            var result = _ideaQueryHandlers.Handle(new GetIdea(Guid.NewGuid()));

            Assert.True(result.IsFailure);
            Assert.Null(result.Value);
        }

        #endregion
    }
}
