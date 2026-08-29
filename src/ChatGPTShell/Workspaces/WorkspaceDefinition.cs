using ChatGPTShell.Panels;

namespace ChatGPTShell.Workspaces;

public sealed class WorkspaceDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Name { get; set; } = "Default";

    public List<ChatPanelDefinition> Panels { get; set; } = new();

    public Guid? ActivePanelId { get; set; }

    public LayoutNodeDefinition? LayoutRoot { get; set; }

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
            ActivePanelId = panel.Id,
            LayoutRoot = LayoutNodeDefinition.ForPanel(panel.Id)
        };
    }

    public bool EnsureUsableLayout()
    {
        if (Panels.Count == 0)
        {
            throw new InvalidOperationException("A workspace must contain at least one panel definition.");
        }

        var panelIds = Panels.Select(panel => panel.Id).ToHashSet();
        var visiblePanelIds = new List<Guid>();
        var seenPanelIds = new HashSet<Guid>();

        if (!TryValidateLayout(LayoutRoot, panelIds, seenPanelIds, visiblePanelIds))
        {
            var fallbackPanelId = ActivePanelId is Guid activePanelId && panelIds.Contains(activePanelId)
                ? activePanelId
                : Panels[0].Id;

            ActivePanelId = fallbackPanelId;
            LayoutRoot = LayoutNodeDefinition.ForPanel(fallbackPanelId);
            return true;
        }

        if (ActivePanelId is not Guid activeId || !seenPanelIds.Contains(activeId))
        {
            ActivePanelId = visiblePanelIds[0];
            return true;
        }

        return false;
    }

    private static bool TryValidateLayout(
        LayoutNodeDefinition? node,
        HashSet<Guid> panelIds,
        HashSet<Guid> seenPanelIds,
        List<Guid> visiblePanelIds)
    {
        if (node is null)
        {
            return false;
        }

        if (node.Kind == LayoutNodeKind.Panel)
        {
            if (node.PanelId is not Guid panelId
                || !panelIds.Contains(panelId)
                || !seenPanelIds.Add(panelId))
            {
                return false;
            }

            visiblePanelIds.Add(panelId);
            return true;
        }

        if (node.Kind != LayoutNodeKind.Split
            || node.First is null
            || node.Second is null
            || !double.IsFinite(node.Ratio)
            || node.Ratio <= 0
            || node.Ratio >= 1)
        {
            return false;
        }

        return TryValidateLayout(node.First, panelIds, seenPanelIds, visiblePanelIds)
            && TryValidateLayout(node.Second, panelIds, seenPanelIds, visiblePanelIds);
    }
}
