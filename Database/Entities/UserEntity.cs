using Database.Helpers;

namespace Database.Entities;

public class UserEntity : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Email { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public UserSettingsEntity UserSettings { get; set; } = default!;

    public override string GetId()
    {
        return IdGenerator.Get(IdLength(), GetType().Name, Name, Email, CreatedDate.Ticks.ToString());
    }

    protected override int IdLength() => 6;
}