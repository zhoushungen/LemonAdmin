namespace Lemon.Domain.Common;

public abstract class Entity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }
}
