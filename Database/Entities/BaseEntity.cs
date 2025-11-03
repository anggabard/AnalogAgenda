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
        if (_id != null) return _id;
        
        // Generate ID using the same logic as before (without PartitionKey)
        _id = IdGenerator.Get(IdLength(), GetType().Name, CreatedDate.Ticks.ToString());
        return _id;
    }
}
