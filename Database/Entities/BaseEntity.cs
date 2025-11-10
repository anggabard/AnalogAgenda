using Database.Helpers;

namespace Database.Entities;

public abstract class BaseEntity
{
    public string Id { get; set; } = string.Empty;

    protected abstract int IdLength();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public virtual string GetId() => IdGenerator.Get(IdLength(), GetType().Name, CreatedDate.Ticks.ToString());
}
