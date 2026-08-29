namespace ChatGPTShell.Roles;

public sealed class PromptCompositionContext
{
    public string ProjectName { get; set; } = string.Empty;

    public string TechStack { get; set; } = string.Empty;

    public string Workflow { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public string ProjectContext { get; set; } = string.Empty;
}
