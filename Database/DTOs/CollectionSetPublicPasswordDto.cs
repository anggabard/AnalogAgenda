namespace Database.DTOs;

/// <summary>Owner-only: set a new public share password without resending photo membership.</summary>
public class CollectionSetPublicPasswordDto
{
    public string PublicPassword { get; set; } = string.Empty;
}
