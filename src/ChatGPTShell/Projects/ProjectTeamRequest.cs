namespace ChatGPTShell.Projects;

public sealed class ProjectTeamRequest
{
    public string Name { get; set; } = "New Project";

    public string TechStack { get; set; } = string.Empty;

    public string Workflow { get; set; } = string.Empty;

    public string ProjectContext { get; set; } = string.Empty;

    public int WorkerCount { get; set; } = 1;

    public bool IncludeManager { get; set; } = true;

    public bool IncludeArchitect { get; set; } = true;

    public bool IncludeReviewer { get; set; } = true;

    public bool IncludeResearcher { get; set; } = true;

    public bool IncludeGeneral { get; set; }
}
