namespace ChatGPTShell.Workspaces;

public enum LayoutNodeKind
{
    Panel,
    Split
}

public enum SplitOrientation
{
    Columns,
    Rows
}

public sealed class LayoutNodeDefinition
{
    public LayoutNodeKind Kind { get; set; } = LayoutNodeKind.Panel;

    public Guid? PanelId { get; set; }

    public SplitOrientation Orientation { get; set; } = SplitOrientation.Columns;

    public double Ratio { get; set; } = 0.5;

    public LayoutNodeDefinition? First { get; set; }

    public LayoutNodeDefinition? Second { get; set; }

    public static LayoutNodeDefinition ForPanel(Guid panelId) => new()
    {
        Kind = LayoutNodeKind.Panel,
        PanelId = panelId
    };

    public static LayoutNodeDefinition Split(
        SplitOrientation orientation,
        LayoutNodeDefinition first,
        LayoutNodeDefinition second,
        double ratio = 0.5) => new()
        {
            Kind = LayoutNodeKind.Split,
            Orientation = orientation,
            Ratio = Math.Clamp(ratio, 0.1, 0.9),
            First = first ?? throw new ArgumentNullException(nameof(first)),
            Second = second ?? throw new ArgumentNullException(nameof(second))
        };
}
