using Google.GenAI;
using Google.GenAI.Types;
using Serilog;

namespace GameVisionTool.Integration.GoogleGemini.Agents;

public static class GenerateResponseHelper
{
    private const int MaxAttempts = 5;

    [ThreadStatic] private static Random? _jitter;

    private static Random Jitter => _jitter ??= new Random(Guid.NewGuid().GetHashCode());

    // Calls Models.GenerateContentAsync, retrying on transient server errors (e.g. the model
    // being temporarily overloaded) with exponential backoff and jitter. Non-transient errors propagate
    // immediately, and the last transient error is rethrown once retries are exhausted.
    public static async Task<string?> GenerateAgentResponse(
        this Client client,
        string model,
        List<Content> contents,
        GenerateContentConfig clientConfig,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await client.Models.GenerateContentAsync(model, contents, clientConfig, cancellationToken);

                // A MAX_TOKENS finish reason means the model was cut off mid-response by the output cap.
                // The text is still returned (partial), but surface it so a silent truncation is visible.
                var finishReason = response.Candidates?.FirstOrDefault()?.FinishReason;
                if (finishReason?.Value == FinishReason.MaxTokens.Value)
                {
                    Log.Warning(
                        "Gemini response for model {Model} was truncated (FinishReason=MAX_TOKENS). Consider raising MaxOutputTokens.",
                        model);
                }

                return response.Text;
            }
            catch (ServerError ex) when (IsTransient(ex) && attempt < MaxAttempts)
            {
                // Exponential backoff (2s, 4s, 8s, ...) plus jitter to avoid a thundering herd
                // of retries hammering an already-overloaded model in lockstep.
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt))
                    + TimeSpan.FromMilliseconds(Jitter.Next(1000));

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    // 503 UNAVAILABLE ("high demand") and 500 INTERNAL are transient and worth retrying;
    // other server errors are surfaced to the caller.
    private static bool IsTransient(ServerError ex) => ex.StatusCode is 503 or 500;
}
