using GameVisionTool.Integration.LlamaSharp;
using GameVisionTool.Integration.LlamaSharp.Agents;
using GameVisionTool.Logic.Application.QueryHandlers.Llama;
using GameVisionTool.Logic.Application.QueryHandlers.Story.Llama;
using GameVisionTool.Messages.Queries.Story.Llama;
using GameVisionTool.Messages.Queries.Llama;
using LLama.Common;
using Xunit;

namespace GameVisionTool.IntegrationTests.Application.QueryHandlerTests.Story
{
    /// <summary>
    /// The Llama generation path, exercised without a .gguf on disk. Before ILlamaAgent existed these
    /// handlers constructed LlamaSharp types directly, so reaching them at all meant loading several GB
    /// of real weights onto a real GPU - which is why nothing covered them.
    ///
    /// A fake rather than the real thing, unlike the data store tests. The guidance there is about
    /// LiteDB specifically: its LINQ translation and whole-document updates diverge from an in-memory
    /// fake, so a fake would make green tests meaningless. Nothing here is checking model behaviour -
    /// what these tests cover is the handler's own work, which is entirely the mapping between a query
    /// and a call, and that mapping is exact either way.
    /// </summary>
    public class LlamaStoryHandlerTests
    {
        private const string ModelPath = @"C:\models\does-not-need-to-exist.gguf";

        private sealed class RecordingLlamaAgent(LlamaGenerationResult result) : ILlamaAgent
        {
            public int Calls { get; private set; }
            public string? ModelPath { get; private set; }
            public uint ContextSize { get; private set; }
            public int GpuLayerCount { get; private set; }
            public int MaxTokens { get; private set; }
            public float Temperature { get; private set; }
            public string? AgentSystemPrompt { get; private set; }
            public bool SuppressThinking { get; private set; }
            public string? StopMarker { get; private set; }
            public string? UserMessage { get; private set; }
            public ChatHistory? ChatHistory { get; private set; }

            public Task<LlamaGenerationResult> GenerateResponse(
                string modelPath,
                uint contextSize,
                int gpuLayerCount,
                int maxTokens,
                float temperature,
                string agentSystemPrompt,
                bool suppressThinking,
                string stopMarker,
                string userMessage,
                ChatHistory? chatHistory)
            {
                Calls++;
                ModelPath = modelPath;
                ContextSize = contextSize;
                GpuLayerCount = gpuLayerCount;
                MaxTokens = maxTokens;
                Temperature = temperature;
                AgentSystemPrompt = agentSystemPrompt;
                SuppressThinking = suppressThinking;
                StopMarker = stopMarker;
                UserMessage = userMessage;
                ChatHistory = chatHistory;

                return Task.FromResult(result);
            }
            
        }

        private sealed class RecordingModelProvider : ILlamaModelProvider
        {
            public int Calls { get; private set; }
            public string? ModelPath { get; private set; }
            public uint ContextSize { get; private set; }
            public int GpuLayerCount { get; private set; }

            public Task EnsureLoaded(string modelPath, uint contextSize, int gpuLayerCount)
            {
                Calls++;
                ModelPath = modelPath;
                ContextSize = contextSize;
                GpuLayerCount = gpuLayerCount;

                return Task.CompletedTask;
            }
        }

        private static GetLlamaAgentResponse Query() =>
            new(ModelPath, "A smuggler with a debt.", contextSize: 8192, gpuLayerCount: 33, maxTokens: 4096,
                temperature: 0.7f, agentSystemPrompt: "You are a novelist.", suppressThinking: true,
                stopMarker: "<<<END>>>", new List<KeyValuePair<string, string>>());

        [Fact]
        public async Task Test_Every_Query_Field_Reaches_The_Agent()
        {
            // Nine positional arguments of four types, several of them adjacent and interchangeable -
            // ContextSize and GpuLayerCount are both numbers, SystemPrompt and Content and StopMarker are
            // all strings. A transposition compiles and would silently generate against the wrong values,
            // so the mapping is asserted field by field rather than by checking a response came back.
            var agent = new RecordingLlamaAgent(new LlamaGenerationResult("A backstory.", WasTruncated: false));

            var result = await new LlamaStoryHandlers(agent).HandleAsync(Query());

            Assert.True(result.IsSuccess);
            Assert.Equal(1, agent.Calls);
            Assert.Equal(ModelPath, agent.ModelPath);
            Assert.Equal(8192u, agent.ContextSize);
            Assert.Equal(33, agent.GpuLayerCount);
            Assert.Equal(4096, agent.MaxTokens);
            Assert.Equal(0.7f, agent.Temperature);
            Assert.Equal("You are a novelist.", agent.AgentSystemPrompt);
            Assert.True(agent.SuppressThinking);
            Assert.Equal("<<<END>>>", agent.StopMarker);

            // The brief is the user turn, not the system prompt. Swapping these two produces a plausible
            // generation from entirely the wrong instructions.
            Assert.Equal("A smuggler with a debt.", agent.UserMessage);
        }

        [Fact]
        public async Task Test_Generated_Text_Is_Returned()
        {
            var agent = new RecordingLlamaAgent(new LlamaGenerationResult("Born on a freighter.", WasTruncated: false));

            var result = await new LlamaStoryHandlers(agent).HandleAsync(Query());

            Assert.True(result.IsSuccess);
            Assert.Equal("Born on a freighter.", result.Value.Response);
            Assert.False(result.Value.WasTruncated);
        }

        [Fact]
        public async Task Test_Truncation_Is_Reported_To_The_Caller()
        {
            // The Backstory page shows a "response cut off" notice off this flag. Dropped here, a
            // backstory that stops mid-sentence just reads as the model writing a bad ending.
            var agent = new RecordingLlamaAgent(new LlamaGenerationResult("Born on a freig", WasTruncated: true));

            var result = await new LlamaStoryHandlers(agent).HandleAsync(Query());

            Assert.True(result.IsSuccess);
            Assert.True(result.Value.WasTruncated);
        }

        [Fact]
        public async Task Test_Loading_A_Model_Warms_The_Provider_And_Echoes_The_Path()
        {
            var provider = new RecordingModelProvider();

            var result = await new LlamaModelQueryHandlers(provider)
                .HandleAsync(new LoadLlamaModel(ModelPath, contextSize: 8192, gpuLayerCount: 33));

            Assert.True(result.IsSuccess);
            Assert.Equal(1, provider.Calls);
            Assert.Equal(ModelPath, provider.ModelPath);
            Assert.Equal(8192u, provider.ContextSize);
            Assert.Equal(33, provider.GpuLayerCount);

            // The page keys its "already requested this one" check off the path that comes back, so an
            // echo of something other than what was asked for would re-dispatch a load on every reload.
            Assert.Equal(ModelPath, result.Value.ModelPath);
        }
    }
}
