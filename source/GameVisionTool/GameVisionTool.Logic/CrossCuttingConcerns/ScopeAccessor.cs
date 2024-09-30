using SimpleInjector;

namespace GameVisionTool.Logic.CrossCuttingConcerns;

public sealed class ScopeAccessor : IAsyncDisposable, IDisposable
{
    public Scope Scope { get; set; }
    public ValueTask DisposeAsync() => this.Scope?.DisposeAsync() ?? default;
    public void Dispose() => this.Scope?.Dispose();
}