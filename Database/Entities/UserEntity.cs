using Database.DBObjects.Enums;
using Database.Helpers;

namespace Database.Entities;

public class UserEntity : BaseEntity
{
    public UserEntity() : base(TableName.Users) { }

    public string Name { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public bool IsSubscraibed { get; set; } = false;

    protected override string GetId()
    {
        return IdGenerator.Get(RowKeyLenght(), PartitionKey, Name, Email, CreatedDate.Ticks.ToString());
    }

    protected override int RowKeyLenght() => 6;
}