namespace Database.Entities;

/// <summary>
/// Interface for entities that have an associated image
/// </summary>
public interface IImageEntity
{
    Guid ImageId { get; set; }
}
