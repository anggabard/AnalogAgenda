using Database.DBObjects.Enums;

namespace Database.Entities;

public class UserEntity : BaseEntity
{
    public UserEntity() : base(TableName.Users) { }

    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    protected override string GetId()
    {
        return Username;
    }

    protected override int RowKeyLenght() => 0; //Not Aplicable
}