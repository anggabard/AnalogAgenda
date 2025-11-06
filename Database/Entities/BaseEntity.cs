using Database.Helpers;

namespace Database.Entities;

public abstract class BaseEntity
{
    private string? _id = null;
    
    public string Id 
    { 
        get => string.IsNullOrEmpty(_id) ? (_id = GetId()) : _id;
        set => _id = value;
    }
    
    protected abstract int IdLength();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    protected virtual string GetId() => IdGenerator.Get(IdLength(), GetType().Name, CreatedDate.Ticks.ToString());
}
