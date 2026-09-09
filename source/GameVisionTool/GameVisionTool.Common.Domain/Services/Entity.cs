namespace GameVisionTool.Common.Domain.Services;

public abstract class Entity<TKey> where TKey : IEquatable<TKey>
{
    public virtual required TKey Id { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
}