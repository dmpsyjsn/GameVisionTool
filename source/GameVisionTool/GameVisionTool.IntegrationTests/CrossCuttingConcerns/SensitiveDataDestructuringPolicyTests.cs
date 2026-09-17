using GameVisionTool.Common.Domain.Services;
using GameVisionTool.Logic.CrossCuttingConcerns;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace GameVisionTool.IntegrationTests.CrossCuttingConcerns;

public class SensitiveDataDestructuringPolicyTests
{
    // Stands in for Serilog's own factory. Scalar-wrapping every value is enough here - these tests
    // are about which properties get replaced with the placeholder, not about how the survivors are
    // rendered.
    private sealed class PassThroughFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
            => new ScalarValue(value);
    }

    // Shaped like the real generation queries: a credential and a length budget on the same message.
    private sealed class GenerationRequest
    {
        public string ApiKey { get; init; } = string.Empty;
        public string AccessToken { get; init; } = string.Empty;
        public int MaxTokens { get; init; }
        public string Content { get; init; } = string.Empty;
    }

    private static Dictionary<string, string?> Destructure(object value)
    {
        var policy = new SensitiveDataDestructuringPolicy();

        Assert.True(policy.TryDestructure(value, new PassThroughFactory(), out var result));

        var structure = Assert.IsType<StructureValue>(result);

        return structure.Properties.ToDictionary(
            x => x.Name,
            x => (x.Value as ScalarValue)?.Value?.ToString());
    }

    [Fact]
    public void Test_MaxTokens_Is_Not_Redacted()
    {
        // "token" is a sensitive fragment so that AccessToken and friends are caught, but MaxTokens
        // is a generation length budget rather than a credential. Redacting it once hid the value
        // needed to diagnose responses silently truncating at the cap.
        var properties = Destructure(new GenerationRequest { ApiKey = "sk-secret", MaxTokens = 9000 });

        Assert.Equal("9000", properties["MaxTokens"]);
    }

    [Fact]
    public void Test_ApiKey_Is_Still_Redacted()
    {
        var properties = Destructure(new GenerationRequest { ApiKey = "sk-secret", MaxTokens = 9000 });

        Assert.Equal(SensitiveDataDestructuringPolicy.RedactedPlaceholder, properties["ApiKey"]);
    }

    [Fact]
    public void Test_A_Genuine_Token_Property_Is_Still_Redacted()
    {
        // The exemption is exact-name only, so anything else carrying "token" fails closed.
        var properties = Destructure(new GenerationRequest { AccessToken = "ya29.secret" });

        Assert.Equal(SensitiveDataDestructuringPolicy.RedactedPlaceholder, properties["AccessToken"]);
    }

    [Fact]
    public void Test_Ordinary_Properties_Pass_Through()
    {
        var properties = Destructure(new GenerationRequest { Content = "write a backstory" });

        Assert.Equal("write a backstory", properties["Content"]);
    }

    [Fact]
    public void Test_An_Empty_Secret_Is_Not_Redacted()
    {
        // Knowing the field was blank is what tells you "my key isn't working" apart from
        // "my key is wrong", and an empty string discloses nothing.
        var properties = Destructure(new GenerationRequest { ApiKey = string.Empty });

        Assert.Equal(string.Empty, properties["ApiKey"]);
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    [Fact]
    public void Test_A_Secret_Nested_In_A_Result_Is_Redacted()
    {
        // The logging decorators log {@returnResult}, so the object reaching the sink is the
        // Result wrapper, not the payload. Result carries no sensitive property names of its own,
        // so the policy declines it and Serilog destructures it by default - and redaction happens
        // only because that default recursion re-consults the policy on the way into Value. A
        // direct TryDestructure call cannot cover that path, hence the real pipeline here.
        //
        // If Result ever grows a secret-named property, or the policy stops opting out of types
        // with nothing sensitive on them, this is the test that fails.
        var sink = new CapturingSink();

        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .WriteTo.Sink(sink)
            .CreateLogger();

        var payload = new GenerationRequest { ApiKey = "sk-secret", MaxTokens = 9000, Content = "write a backstory" };

        logger.Debug("Command Result = {@returnResult}", Result.Ok(payload));

        var wrapper = Assert.IsType<StructureValue>(Assert.Single(sink.Events).Properties["returnResult"]);
        var value = Assert.IsType<StructureValue>(Property(wrapper, "Value"));

        Assert.Equal(SensitiveDataDestructuringPolicy.RedactedPlaceholder, Scalar(value, "ApiKey"));

        // The rest of the graph has to survive, or the log is redacted into uselessness.
        Assert.Equal(9000, Scalar(value, "MaxTokens"));
        Assert.Equal("write a backstory", Scalar(value, "Content"));

        // Belt and braces: the secret must not reach the sink by any other route, such as a
        // ToString() fallback on the wrapper.
        Assert.DoesNotContain("sk-secret", sink.Events.Single().RenderMessage());
    }

    private static LogEventPropertyValue Property(StructureValue structure, string name)
        => structure.Properties.Single(x => x.Name == name).Value;

    private static object? Scalar(StructureValue structure, string name)
        => Assert.IsType<ScalarValue>(Property(structure, name)).Value;
}
