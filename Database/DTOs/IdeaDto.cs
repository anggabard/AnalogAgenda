namespace Database.DTOs;

public class IdeaSessionSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayLabel { get; set; } = string.Empty;
}

public class IdeaDto
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Outcome { get; set; } = string.Empty;

    public List<string> ConnectedSessionIds { get; set; } = [];

    public List<IdeaSessionSummaryDto> ConnectedSessions { get; set; } = [];
}
