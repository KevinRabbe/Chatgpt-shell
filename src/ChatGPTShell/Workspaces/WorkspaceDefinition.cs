using ChatGPTShell.Panels;

namespace ChatGPTShell.Workspaces;

public sealed class WorkspaceDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    public List<ChatPanelDefinition> Panels { get; set; } = new();

    public Guid? ActivePanelId { get; set; }

    public static WorkspaceDefinition CreateDefault()
    {
        var panel = new ChatPanelDefinition
        {
            Title = "ChatGPT",
            ConversationUrl = "https://chatgpt.com/"
        };

        return new WorkspaceDefinition
        {
            Panels = new List<ChatPanelDefinition> { panel },
            ActivePanelId = panel.Id
        };
    }
}
