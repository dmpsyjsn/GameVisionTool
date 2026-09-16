using GameVisionTool.Common.Domain;
using GameVisionTool.Common.Domain.Services;
using GameVisionTool.IntegrationTests.Helpers;
using GameVisionTool.Logic.Application.CommandHandlers.Agents;
using GameVisionTool.Logic.Application.QueryHandlers.Agents;
using GameVisionTool.Logic.Domain.AgentSettings;
using GameVisionTool.Messages.Commands.Agents;
using GameVisionTool.Messages.Queries.Agents;
using GameVisionTool.Persistence.LiteDb;
using LiteDB;
using Xunit;

namespace GameVisionTool.IntegrationTests.Application.CommandHandlerTests.Agents
{
    /// <summary>
    /// The stop marker is the string an agent's system prompt is told to end with, and which generation
    /// stops on. It only works if the exact text the user typed survives the round trip to LiteDB and
    /// back - it is matched as a plain substring of the model's output, so a marker that comes back
    /// altered, or as null, silently stops matching and the agent goes back to running to its token cap.
    /// </summary>
    public class LlamaAgentStopMarkerTests : CustomLiteDbTestDriver
    {
        private const string StopMarker = "<<<END>>>";

        private readonly IDataStore<LlamaAgentSettings, Guid> _agentStore;
        private readonly LlamaAgentHandlers _handlers;
        private readonly LlamaAgentQueryHandlers _queryHandlers;

        public LlamaAgentStopMarkerTests()
        {
            _agentStore = new LiteDbDataStore<LlamaAgentSettings, Guid>(Db);

            _handlers = new LlamaAgentHandlers(_agentStore);
            _queryHandlers = new LlamaAgentQueryHandlers(_agentStore);
        }

        private Result<Guid> AddAgent(string stopMarker = StopMarker) =>
            _handlers.Handle(new AddLlamaAgent(
                StringHelpers.GenerateRandomString(),
                AgentGroupType.Backstory,
                StringHelpers.GenerateRandomString(),
                stopMarker: stopMarker));

        [Fact]
        public void Test_Add_Llama_Agent_Stores_The_Stop_Marker()
        {
            var added = AddAgent();

            var item = _agentStore.GetByIdOrDefault(added.Value);

            Assert.NotNull(item);
            Assert.Equal(StopMarker, item.StopMarker);
        }

        [Fact]
        public void Test_Add_Llama_Agent_Without_A_Stop_Marker_Stores_Empty_Not_Null()
        {
            // Blank is how an agent says "rely on the model ending its own turn". It has to arrive as
            // an empty string rather than null: the form calls Trim() on whatever comes back.
            var added = _handlers.Handle(new AddLlamaAgent(
                StringHelpers.GenerateRandomString(),
                AgentGroupType.Backstory,
                StringHelpers.GenerateRandomString()));

            var item = _agentStore.GetByIdOrDefault(added.Value);

            Assert.NotNull(item);
            Assert.Equal(string.Empty, item.StopMarker);
        }

        [Fact]
        public void Test_Update_Llama_Agent_Changes_The_Stop_Marker()
        {
            const string updatedMarker = "###FINISHED###";

            var added = AddAgent();

            var result = _handlers.Handle(new UpdateLlamaAgent(
                added.Value,
                StringHelpers.GenerateRandomString(),
                AgentGroupType.Backstory,
                StringHelpers.GenerateRandomString(),
                stopMarker: updatedMarker));

            Assert.True(result.IsSuccess);
            Assert.Equal(updatedMarker, _agentStore.GetById(added.Value).StopMarker);
        }

        [Fact]
        public void Test_Update_Llama_Agent_Can_Clear_The_Stop_Marker()
        {
            // Update replaces the whole document, so clearing the field has to actually clear it -
            // a marker that survived being blanked would keep cutting responses short.
            var added = AddAgent();

            _handlers.Handle(new UpdateLlamaAgent(
                added.Value,
                StringHelpers.GenerateRandomString(),
                AgentGroupType.Backstory,
                StringHelpers.GenerateRandomString(),
                stopMarker: string.Empty));

            Assert.Equal(string.Empty, _agentStore.GetById(added.Value).StopMarker);
        }

        [Fact]
        public void Test_Agent_Stored_Before_The_Field_Existed_Reads_Back_Empty_Not_Null()
        {
            // Every agent already in the user's database predates this field, so its document has no
            // StopMarker at all. LiteDB leaves an absent field at whatever the property initializer
            // set, and the whole UI path assumes that is a string - Trim() on a null would throw the
            // moment such an agent was opened for editing.
            var id = Guid.NewGuid();

            // The untyped collection, so the document can be written without the field the entity now
            // declares. LiteDbDataStore names its collection after the entity type.
            Db.GetCollection(nameof(LlamaAgentSettings)).Insert(new BsonDocument
            {
                ["_id"] = id,
                ["Name"] = "Stored before StopMarker existed",
                ["GroupType"] = (int)AgentGroupType.Backstory,
                ["SystemPrompt"] = "Anything.",
                ["MaxTokens"] = 8192,
                ["Temperature"] = 0.9d,
                ["SuppressThinking"] = false,
            });

            var item = _agentStore.GetByIdOrDefault(id);

            Assert.NotNull(item);
            Assert.NotNull(item.StopMarker);
            Assert.Equal(string.Empty, item.StopMarker);
        }

        [Fact]
        public void Test_Query_Handlers_Project_The_Stop_Marker()
        {
            // The generation path reads the marker off AgentViewModel, so a projection that dropped it
            // would disable the stop sequence while the agent still looked configured in the list.
            var added = AddAgent();

            var single = _queryHandlers.Handle(new GetLlamaAgent(added.Value));

            Assert.True(single.IsSuccess);
            Assert.Equal(StopMarker, single.Value.StopMarker);

            var all = _queryHandlers.Handle(new GetLlamaAgents());

            Assert.True(all.IsSuccess);
            Assert.Equal(StopMarker, Assert.Single(all.Value.Items).StopMarker);
        }

        [Fact]
        public void Test_Stop_Marker_Survives_Verbatim()
        {
            // Matched as an exact substring of decoded model output, so anything the round trip alters -
            // whitespace, angle brackets, case - stops it matching.
            const string awkwardMarker = "<<< END OF PROFILE >>>";

            var added = AddAgent(awkwardMarker);

            Assert.Equal(awkwardMarker, _agentStore.GetById(added.Value).StopMarker);
        }
    }
}
