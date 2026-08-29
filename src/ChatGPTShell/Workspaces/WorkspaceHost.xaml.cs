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

    private readonly Dictionary<Guid, ChatPanel> _livePanels = new();
    private WorkspaceDefinition? _workspace;
    private WebViewEnvironmentService? _environmentService;
    private Guid? _focusedPanelId;

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
        Render();

        if (layoutRepaired)
        {
            LayoutChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Render()
    {
        if (_workspace is null || _environmentService is null)
        {
            throw new InvalidOperationException("The workspace host must be configured before rendering.");
        }

        var renderRoot = _focusedPanelId is Guid focusedPanelId
            ? LayoutNodeDefinition.ForPanel(focusedPanelId)
            : _workspace.LayoutRoot!;

        var requiredPanelIds = LayoutTreeEditor.GetVisiblePanelIds(renderRoot).ToHashSet();

        foreach (var entry in _livePanels.ToArray())
        {
            DetachPanel(entry.Value);

            if (!requiredPanelIds.Contains(entry.Key))
            {
                entry.Value.Dispose();
                _livePanels.Remove(entry.Key);
            }
        }

        HostRoot.Children.Clear();
        HostRoot.Children.Add(BuildNode(renderRoot));

        var canClose = LayoutTreeEditor.GetVisiblePanelIds(_workspace.LayoutRoot).Count > 1;

        foreach (var panel in _livePanels.Values)
        {
            panel.SetCloseEnabled(canClose);
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

            return GetOrCreatePanel(panelId);
        }

        if (node.First is null || node.Second is null)
        {
            throw new InvalidOperationException("A split layout node must contain two child nodes.");
        }

        return node.Orientation == SplitOrientation.Columns
            ? BuildColumnSplit(node)
            : BuildRowSplit(node);
    }

    private ChatPanel GetOrCreatePanel(Guid panelId)
    {
        if (_workspace is null || _environmentService is null)
        {
            throw new InvalidOperationException("The workspace host must be configured before creating panels.");
        }

        if (_livePanels.TryGetValue(panelId, out var existingPanel))
        {
            return existingPanel;
        }

        var definition = _workspace.Panels.Single(panel => panel.Id == panelId);
        var panel = new ChatPanel();
        panel.DefinitionChanged += OnPanelDefinitionChanged;
        panel.AddRequested += OnPanelAddRequested;
        panel.FocusRequested += OnPanelFocusRequested;
        panel.CloseRequested += OnPanelCloseRequested;
        panel.Configure(definition, _environmentService);

        _livePanels.Add(panelId, panel);
        return panel;
    }

    private Grid BuildColumnSplit(LayoutNodeDefinition node)
    {
        var grid = new Grid();
        var ratio = NormalizeRatio(node);

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
        var ratio = NormalizeRatio(node);

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

    private static double NormalizeRatio(LayoutNodeDefinition node)
    {
        var ratio = Math.Clamp(node.Ratio, 0.1, 0.9);
        node.Ratio = ratio;
        return ratio;
    }

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

    private void OnPanelAddRequested(object? sender, EventArgs e)
    {
        if (sender is not ChatPanel panel || _workspace is null)
        {
            return;
        }

        var menu = new ContextMenu
        {
            Placement = PlacementMode.MousePoint
        };

        menu.Items.Add(CreateMenuItem(
            "New chat beside",
            () => OpenNewPanel(panel.PanelId, SplitOrientation.Columns)));

        menu.Items.Add(CreateMenuItem(
            "New chat below",
            () => OpenNewPanel(panel.PanelId, SplitOrientation.Rows)));

        var visiblePanelIds = LayoutTreeEditor.GetVisiblePanelIds(_workspace.LayoutRoot).ToHashSet();
        var dormantPanels = _workspace.Panels
            .Where(definition => !visiblePanelIds.Contains(definition.Id))
            .ToList();

        if (dormantPanels.Count > 0)
        {
            menu.Items.Add(new Separator());

            var reopenMenu = new MenuItem
            {
                Header = "Reopen saved chat"
            };

            foreach (var definition in dormantPanels)
            {
                var savedPanelId = definition.Id;
                reopenMenu.Items.Add(CreateMenuItem(
                    definition.Title,
                    () => OpenExistingPanel(panel.PanelId, savedPanelId)));
            }

            menu.Items.Add(reopenMenu);
        }

        menu.IsOpen = true;
    }

    private void OnPanelFocusRequested(object? sender, EventArgs e)
    {
        if (sender is not ChatPanel panel)
        {
            return;
        }

        _focusedPanelId = _focusedPanelId == panel.PanelId
            ? null
            : panel.PanelId;

        Render();
    }

    private void OnPanelCloseRequested(object? sender, EventArgs e)
    {
        if (sender is not ChatPanel panel)
        {
            return;
        }

        var panelId = panel.PanelId;
        Dispatcher.BeginInvoke(new Action(() => ClosePanel(panelId)));
    }

    private void ClosePanel(Guid panelId)
    {
        if (_workspace is null || !LayoutTreeEditor.TryClosePanel(_workspace, panelId))
        {
            return;
        }

        if (_focusedPanelId == panelId)
        {
            _focusedPanelId = null;
        }

        Render();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OpenNewPanel(Guid targetPanelId, SplitOrientation orientation)
    {
        if (_workspace is null)
        {
            return;
        }

        var definition = new ChatPanelDefinition
        {
            Title = GetNextChatTitle(),
            ConversationUrl = "https://chatgpt.com/"
        };

        _workspace.Panels.Add(definition);

        if (!LayoutTreeEditor.TrySplitPanel(_workspace, targetPanelId, definition.Id, orientation))
        {
            _workspace.Panels.Remove(definition);
            return;
        }

        _focusedPanelId = null;
        Render();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OpenExistingPanel(Guid targetPanelId, Guid panelIdToOpen)
    {
        if (_workspace is null
            || !LayoutTreeEditor.TrySplitPanel(
                _workspace,
                targetPanelId,
                panelIdToOpen,
                SplitOrientation.Columns))
        {
            return;
        }

        _focusedPanelId = null;
        Render();
        LayoutChanged?.Invoke(this, EventArgs.Empty);
    }

    private string GetNextChatTitle()
    {
        if (_workspace is null)
        {
            return "Chat";
        }

        var existingTitles = _workspace.Panels
            .Select(panel => panel.Title)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var number = _workspace.Panels.Count + 1;
        var title = $"Chat {number}";

        while (existingTitles.Contains(title))
        {
            number++;
            title = $"Chat {number}";
        }

        return title;
    }

    private static MenuItem CreateMenuItem(string header, Action action)
    {
        var item = new MenuItem
        {
            Header = header
        };

        item.Click += (_, _) => action();
        return item;
    }

    private static void DetachPanel(ChatPanel panel)
    {
        if (VisualTreeHelper.GetParent(panel) is Panel parent)
        {
            parent.Children.Remove(panel);
        }
    }
}
