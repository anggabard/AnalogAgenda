namespace Configuration.Sections;

public class Smtp
{
    public string Host { get; set; } = default!;
    public int Port { get; set; }
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}
