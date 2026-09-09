using System.Collections.Specialized;
using Google.GenAI;

namespace GameVisionTool.Integration.GoogleGemini.ClientConfiguration;

// Builds the Gemini Client from the host's app settings. Takes the settings collection
// rather than reading ConfigurationManager directly, matching how IntuitQuickBooksService is wired
// from the composition root — this project stays free of System.Configuration.
public class GeminiClientBuilder
{
    public Client GetClient(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("The 'GoogleGeminiApiKey' app setting is missing or empty.");
        }

        // ToDo: Just setting the API key for now, but we will need to implement a more secure way of handling this in the future.
        var client = new Client(apiKey: apiKey);
        return client;
    }
}
