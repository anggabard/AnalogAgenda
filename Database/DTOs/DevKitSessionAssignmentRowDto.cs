namespace Database.DTOs;

public class DevKitSessionAssignmentRowDto
{
    public string Id { get; set; } = string.Empty;

    public DateOnly SessionDate { get; set; }

    public string Location { get; set; } = string.Empty;

    public string ParticipantsPreview { get; set; } = string.Empty;

    public bool IsSelected { get; set; }
}
