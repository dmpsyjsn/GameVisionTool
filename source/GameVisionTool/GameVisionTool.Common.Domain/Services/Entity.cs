using NodaTime;
using System.Diagnostics.CodeAnalysis;

namespace GameVisionTool.Common.Domain.Services;

public abstract class Entity
{
    public virtual required int Id { get; set; }
    public Instant CreatedOn { get; set; }
    public Instant ModifiedOn { get; set; }
}