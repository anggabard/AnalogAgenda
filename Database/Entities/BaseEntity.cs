using Database.Helpers;

namespace Database.Entities;

public abstract class BaseEntity
{
    private string? _id;
    
    public string Id 
    { 
        get => _id ?? GetId();
        set => _id = value;
    }
    
    protected abstract int IdLength();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    protected virtual string GetId()
    {
        return IdGenerator.Get(IdLength(), GetType().Name, CreatedDate.Ticks.ToString());
    }
}
