namespace ChatGPTShell.Roles;

public sealed class RoleDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = "1.0";

    public string Description { get; set; } = string.Empty;

    public string PromptTemplate { get; set; } = string.Empty;

    public List<string> DefaultContextModuleIds { get; set; } = new();
}

public sealed class RoleLibraryDocument
{
    public int SchemaVersion { get; set; } = 1;

    public List<RoleDefinition> Roles { get; set; } = new();
}
