namespace ChatGPTShell.Workspaces;

public static class LayoutTreeEditor
{
    public static IReadOnlyList<Guid> GetVisiblePanelIds(LayoutNodeDefinition? root)
    {
        var result = new List<Guid>();
        CollectPanelIds(root, result);
        return result;
    }

    public static bool TrySplitPanel(
        WorkspaceDefinition workspace,
        Guid targetPanelId,
        Guid panelIdToOpen,
        SplitOrientation orientation)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        workspace.EnsureUsableLayout();

        var visiblePanelIds = GetVisiblePanelIds(workspace.LayoutRoot);

        if (!visiblePanelIds.Contains(targetPanelId)
            || visiblePanelIds.Contains(panelIdToOpen)
            || workspace.Panels.All(panel => panel.Id != panelIdToOpen))
        {
            return false;
        }

        var replacement = LayoutNodeDefinition.Split(
            orientation,
            LayoutNodeDefinition.ForPanel(targetPanelId),
            LayoutNodeDefinition.ForPanel(panelIdToOpen));

        workspace.LayoutRoot = ReplacePanel(
            workspace.LayoutRoot!,
            targetPanelId,
            replacement,
            out var replaced);

        if (replaced)
        {
            workspace.ActivePanelId = panelIdToOpen;
        }

        return replaced;
    }

    public static bool TryClosePanel(WorkspaceDefinition workspace, Guid panelId)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        workspace.EnsureUsableLayout();

        var visiblePanelIds = GetVisiblePanelIds(workspace.LayoutRoot);

        if (visiblePanelIds.Count <= 1 || !visiblePanelIds.Contains(panelId))
        {
            return false;
        }

        var newRoot = RemovePanel(workspace.LayoutRoot!, panelId, out var removed);

        if (!removed || newRoot is null)
        {
            return false;
        }

        workspace.LayoutRoot = newRoot;

        var remainingPanelIds = GetVisiblePanelIds(newRoot);

        if (workspace.ActivePanelId == panelId
            || workspace.ActivePanelId is not Guid activePanelId
            || !remainingPanelIds.Contains(activePanelId))
        {
            workspace.ActivePanelId = remainingPanelIds[0];
        }

        return true;
    }

    private static LayoutNodeDefinition ReplacePanel(
        LayoutNodeDefinition node,
        Guid targetPanelId,
        LayoutNodeDefinition replacement,
        out bool replaced)
    {
        if (node.Kind == LayoutNodeKind.Panel)
        {
            replaced = node.PanelId == targetPanelId;
            return replaced ? replacement : node;
        }

        var first = ReplacePanel(node.First!, targetPanelId, replacement, out replaced);

        if (replaced)
        {
            node.First = first;
            return node;
        }

        var second = ReplacePanel(node.Second!, targetPanelId, replacement, out replaced);

        if (replaced)
        {
            node.Second = second;
        }

        return node;
    }

    private static LayoutNodeDefinition? RemovePanel(
        LayoutNodeDefinition node,
        Guid targetPanelId,
        out bool removed)
    {
        if (node.Kind == LayoutNodeKind.Panel)
        {
            removed = node.PanelId == targetPanelId;
            return removed ? null : node;
        }

        var first = RemovePanel(node.First!, targetPanelId, out removed);

        if (removed)
        {
            if (first is null)
            {
                return node.Second;
            }

            node.First = first;
            return node;
        }

        var second = RemovePanel(node.Second!, targetPanelId, out removed);

        if (removed)
        {
            if (second is null)
            {
                return node.First;
            }

            node.Second = second;
        }

        return node;
    }

    private static void CollectPanelIds(LayoutNodeDefinition? node, List<Guid> result)
    {
        if (node is null)
        {
            return;
        }

        if (node.Kind == LayoutNodeKind.Panel)
        {
            if (node.PanelId is Guid panelId)
            {
                result.Add(panelId);
            }

            return;
        }

        CollectPanelIds(node.First, result);
        CollectPanelIds(node.Second, result);
    }
}
