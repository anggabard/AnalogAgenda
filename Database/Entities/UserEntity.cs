using Database.DBObjects.Enums;

namespace Database.Entities;

public class UserEntity : BaseEntity
{
    public UserEntity() : base(TableName.Users) { }

    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    protected override string GetId() => throw new Exception("Not aplicable"); //The RowKey is the email and the entries are added manually

    protected override int RowKeyLenght() => throw new Exception("Not aplicable"); //The RowKey is the email and its variable
}