namespace Configuration.Sections;

public class ContainerRegistry
{
    public required string Name { get; set; }
    public required List<string> RepositoryNames { get; set; }
}

