namespace GameVisionTool.Common.Domain.Queries;

public interface IQuery<TResult>;

public interface IAmALlamaSharpQuery<TResult> : IQuery<TResult>
{
    public string ModelPath { get; }
}