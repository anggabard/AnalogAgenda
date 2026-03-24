namespace Database.Entities;

public class IdeaPhotoEntity
{
    public string IdeaId { get; set; } = string.Empty;

    public string PhotoId { get; set; } = string.Empty;

    public IdeaEntity? Idea { get; set; }

    public PhotoEntity? Photo { get; set; }
}
