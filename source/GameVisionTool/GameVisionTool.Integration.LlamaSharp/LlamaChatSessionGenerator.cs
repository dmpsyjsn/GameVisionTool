using LLama;
using LLama.Common;
using LLama.Transformers;

namespace GameVisionTool.Integration.LlamaSharp;

public sealed class LlamaChatSessionGenerator(LLamaWeights model, ModelParams parameters) : IDisposable
{
    private readonly LLamaContext _context = model.CreateContext(parameters);

    public ChatSession GenerateChatSession(ChatHistory chatHistory)
    {
        var executor = new InteractiveExecutor(_context);

        var session = new ChatSession(executor, chatHistory);

        // add the default templator. If llama.cpp doesn't support the template by default, 
        // you'll need to write your own transformer to format the prompt correctly
        session.WithHistoryTransform(new PromptTemplateTransformer(model, withAssistant: true));

        // Strip the stray replacement character llama 3 emits around its end-of-turn token. "User:"
        // used to be filtered here too, but PromptTemplateTransformer renders turns as the model's
        // own special tokens - so that string never marks a turn boundary, and filtering it would
        // only corrupt a backstory that legitimately contained the word.
        session.WithOutputTransform(new LLamaTransforms.KeywordTextOutputStreamTransform(
            ["�"],
            redundancyLength: 5));

        return session;
    }

    // Token count for the loaded model's own tokenizer, used to work out how much of the prompt has
    // to survive context shifting.
    public int CountTokens(string text) => _context.Tokenize(text, addBos: true, special: true).Length;

    public ChatHistory GenerateSystemPromptTemplate(string template)
    {
        var history = new ChatHistory();
        history.AddMessage(AuthorRole.System, template);
        return history;
    }

    public void Dispose() => _context.Dispose();
}