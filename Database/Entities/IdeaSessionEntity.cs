namespace Database.Entities;

public class IdeaSessionEntity
{
    public string IdeaId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;

    public IdeaEntity? Idea { get; set; }
    public SessionEntity? Session { get; set; }
}
