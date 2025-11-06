using Database.Helpers;

namespace Database.Entities;

public class UserEntity : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public bool IsSubscribed { get; set; } = false;

    protected override string GetId()
    {
        return IdGenerator.Get(IdLength(), GetType().Name, Name, Email, CreatedDate.Ticks.ToString());
    }

    protected override int IdLength() => 6;
}