using Database.Helpers;

namespace Database.Entities;

public class KeyEntity : BaseEntity
{
    public required string Key { get; set; }
    public DateTime ExpirationDate { get; set; }

    protected override int IdLength() => 4;
}

