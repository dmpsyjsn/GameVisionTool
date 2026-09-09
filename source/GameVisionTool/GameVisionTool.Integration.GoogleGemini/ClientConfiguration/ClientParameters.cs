namespace GameVisionTool.Integration.GoogleGemini.ClientConfiguration;

public class ClientParameters
{
    private readonly Dictionary<string, string> _parameters = new();

    public void SetParameter(string key, string value)
    {
        _parameters[key] = value;
    }

    public void SetParameters(Dictionary<string, string> parameters)
    {
        foreach (var kvp in parameters)
        {
            SetParameter(kvp.Key, kvp.Value);
        }
    }

    // Indexer access; throws KeyNotFoundException if the key was never set.
    public string this[string key] => _parameters[key];
}
