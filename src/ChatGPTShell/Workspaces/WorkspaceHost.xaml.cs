using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ChatGPTShell.Panels;
using ChatGPTShell.Web;

namespace ChatGPTShell.Workspaces;

public partial class WorkspaceHost : UserControl
{
    private static readonly Brush SplitterBrush = new SolidColorBrush(Color.FromRgb(42, 42, 42));

    private WorkspaceDefinition? _workspace;
    private WebViewEnvironmentService? _environmentService;

    public WorkspaceHost()
    {
        InitializeComponent();
    }

    public event EventHandler? DefinitionChanged;

    public event EventHandler? LayoutChanged;

    public void Configure(
        WorkspaceDefinition workspace,
        WebViewEnvironmentService environmentService)
    {
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _environmentService = environmentService ?? throw new ArgumentNullException(nameof(environmentService));

        var layoutRepaired = workspace.EnsureUsableLayout();

        HostRoot.Children.Clear();
        HostRoot.Children.Add(BuildNode(workspace.LayoutRoot!));

        if (layoutRepaired)
        {
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private FrameworkElement BuildNode(LayoutNodeDefinition node)
    {
        if (_workspace is null || _environmentService is null)
        {
            throw new InvalidOperationException("The workspace host must be configured before rendering.");
        }

        if (node.Kind == LayoutNodeKind.Panel)
        {
            var panelId = node.PanelId
                ?? throw new InvalidOperationException("A panel layout node must reference a panel id.");

            var definition = _workspace.Panels.Single(panel => panel.Id == panelId);
            var panel = new ChatPanel();
            panel.DefinitionChanged += OnPanelDefinitionChanged;
            panel.Configure(definition, _environmentService);
            return panel;
        }

        if (node.First is null || node.Second is null)
        {
            throw new InvalidOperationException("A split layout node must contain two child nodes.");
        }

        return node.Orientation == SplitOrientation.Columns
            ? BuildColumnSplit(node)
            : BuildRowSplit(node);
    }

    private Grid BuildColumnSplit(LayoutNodeDefinition node)
    {
        var grid = new Grid();
        var ratio = Math.Clamp(node.Ratio, 0.1, 0.9);

        var firstColumn = new ColumnDefinition
        {
            Width = new GridLength(ratio, GridUnitType.Star)
        };
        var splitterColumn = new ColumnDefinition
        {
            Width = new GridLength(5)
        };
        var secondColumn = new ColumnDefinition
        {
            Width = new GridLength(1 - ratio, GridUnitType.Star)
        };

        grid.ColumnDefinitions.Add(firstColumn);
        grid.ColumnDefinitions.Add(splitterColumn);
        grid.ColumnDefinitions.Add(secondColumn);

        var first = BuildNode(node.First!);
        var second = BuildNode(node.Second!);
        var splitter = CreateSplitter(GridResizeDirection.Columns, Cursors.SizeWE);

        Grid.SetColumn(first, 0);
        Grid.SetColumn(splitter, 1);
        Grid.SetColumn(second, 2);

        splitter.DragCompleted += (_, _) =>
            UpdateColumnRatio(node, firstColumn, secondColumn);

        grid.Children.Add(first);
        grid.Children.Add(splitter);
        grid.Children.Add(second);

        return grid;
    }

    private Grid BuildRowSplit(LayoutNodeDefinition node)
    {
        var grid = new Grid();
        var ratio = Math.Clamp(node.Ratio, 0.1, 0.9);

        var firstRow = new RowDefinition
        {
            Height = new GridLength(ratio, GridUnitType.Star)
        };
        var splitterRow = new RowDefinition
        {
            Height = new GridLength(5)
        };
        var secondRow = new RowDefinition
        {
            Height = new GridLength(1 - ratio, GridUnitType.Star)
        };

        grid.RowDefinitions.Add(firstRow);
        grid.RowDefinitions.Add(splitterRow);
        grid.RowDefinitions.Add(secondRow);

        var first = BuildNode(node.First!);
        var second = BuildNode(node.Second!);
        var splitter = CreateSplitter(GridResizeDirection.Rows, Cursors.SizeNS);

        Grid.SetRow(first, 0);
        Grid.SetRow(splitter, 1);
        Grid.SetRow(second, 2);

        splitter.DragCompleted += (_, _) =>
            UpdateRowRatio(node, firstRow, secondRow);

        grid.Children.Add(first);
        grid.Children.Add(splitter);
        grid.Children.Add(second);

        return grid;
    }

    private static GridSplitter CreateSplitter(GridResizeDirection direction, Cursor cursor) => new()
    {
        Background = SplitterBrush,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
        ResizeDirection = direction,
        ResizeBehavior = GridResizeBehavior.PreviousAndNext,
        ShowsPreview = false,
        Cursor = cursor
    };

    private void UpdateColumnRatio(
        LayoutNodeDefinition node,
        ColumnDefinition first,
        ColumnDefinition second)
    {
        var totalWidth = first.ActualWidth + second.ActualWidth;

        if (totalWidth <= 0)
        {
            return;
        }

        UpdateRatio(node, first.ActualWidth / totalWidth);
    }

    private void UpdateRowRatio(
        LayoutNodeDefinition node,
        RowDefinition first,
        RowDefinition second)
    {
        var totalHeight = first.ActualHeight + second.ActualHeight;

        if (totalHeight <= 0)
        {
            return;
        }

        UpdateRatio(node, first.ActualHeight / totalHeight);
    }

    private void UpdateRatio(LayoutNodeDefinition node, double ratio)
    {
        var clampedRatio = Math.Clamp(ratio, 0.1, 0.9);

        if (Math.Abs(node.Ratio - clampedRatio) < 0.0001)
        {
            return;
        }

        node.Ratio = clampedRatio;
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnPanelDefinitionChanged(object? sender, EventArgs e)
    {
        DefinitionChanged?.Invoke(this, EventArgs.Empty);
    }
}
