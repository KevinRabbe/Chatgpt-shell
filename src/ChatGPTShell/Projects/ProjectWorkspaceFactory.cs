using ChatGPTShell.Panels;
using ChatGPTShell.Roles;
using ChatGPTShell.Workspaces;

namespace ChatGPTShell.Projects;

public static class ProjectWorkspaceFactory
{
    public const int MaxInitialLivePanels = 4;
    public const int MaxWorkerCount = 32;

    public static WorkspaceDefinition Create(
        ProjectTeamRequest request,
        RoleLibraryDocument roleLibrary)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(roleLibrary);

        ValidateRequest(request);

        var roles = roleLibrary.Roles.ToDictionary(
            role => role.Id,
            StringComparer.OrdinalIgnoreCase);

        var project = new ProjectProfile
        {
            Name = request.Name.Trim(),
            TechStack = request.TechStack.Trim(),
            Workflow = request.Workflow.Trim(),
            Context = request.ProjectContext.Trim()
        };

        var panels = new List<ChatPanelDefinition>();

        if (request.IncludeManager)
        {
            panels.Add(CreatePanel(roles, "manager", "Project Manager", project));
        }

        for (var workerIndex = 1; workerIndex <= request.WorkerCount; workerIndex++)
        {
            panels.Add(CreatePanel(
                roles,
                "worker",
                $"Worker {workerIndex}",
                project));
        }

        if (request.IncludeArchitect)
        {
            panels.Add(CreatePanel(roles, "architect", "Architect", project));
        }

        if (request.IncludeReviewer)
        {
            panels.Add(CreatePanel(roles, "reviewer", "Reviewer", project));
        }

        if (request.IncludeResearcher)
        {
            panels.Add(CreatePanel(roles, "researcher", "Research", project));
        }

        if (request.IncludeGeneral)
        {
            panels.Add(CreatePanel(roles, "general", "General", project));
        }

        if (panels.Count == 0)
        {
            throw new InvalidOperationException("A project workspace must contain at least one role panel.");
        }

        var visiblePanels = panels.Take(MaxInitialLivePanels).ToList();
        var layoutRoot = BuildStarterLayout(visiblePanels.Select(panel => panel.Id).ToList());

        return new WorkspaceDefinition
        {
            Name = project.Name,
            Project = project,
            Panels = panels,
            ActivePanelId = visiblePanels[0].Id,
            LayoutRoot = layoutRoot
        };
    }

    private static ChatPanelDefinition CreatePanel(
        IReadOnlyDictionary<string, RoleDefinition> roles,
        string roleId,
        string title,
        ProjectProfile project,
        string? specialization = null)
    {
        if (!roles.TryGetValue(roleId, out var role))
        {
            throw new InvalidOperationException(
                $"The project template requires role '{roleId}', but the role library does not contain it.");
        }

        var normalizedSpecialization = string.IsNullOrWhiteSpace(specialization)
            ? null
            : specialization.Trim();

        var prompt = PromptComposer.Compose(
            role,
            new PromptCompositionContext
            {
                ProjectName = project.Name,
                TechStack = project.TechStack,
                Workflow = project.Workflow,
                Specialization = normalizedSpecialization ?? string.Empty,
                ProjectContext = project.Context
            });

        return new ChatPanelDefinition
        {
            Title = title,
            ConversationUrl = "https://chatgpt.com/",
            RoleId = role.Id,
            RoleVersion = role.Version,
            Specialization = normalizedSpecialization,
            PendingBootstrapPrompt = prompt
        };
    }

    private static LayoutNodeDefinition BuildStarterLayout(IReadOnlyList<Guid> panelIds)
    {
        if (panelIds.Count == 0)
        {
            throw new ArgumentException("At least one visible panel id is required.", nameof(panelIds));
        }

        if (panelIds.Count == 1)
        {
            return LayoutNodeDefinition.ForPanel(panelIds[0]);
        }

        return LayoutNodeDefinition.Split(
            SplitOrientation.Columns,
            LayoutNodeDefinition.ForPanel(panelIds[0]),
            BuildVerticalStack(panelIds.Skip(1).ToList()),
            ratio: 0.62);
    }

    private static LayoutNodeDefinition BuildVerticalStack(IReadOnlyList<Guid> panelIds)
    {
        if (panelIds.Count == 1)
        {
            return LayoutNodeDefinition.ForPanel(panelIds[0]);
        }

        var firstShare = 1.0 / panelIds.Count;

        return LayoutNodeDefinition.Split(
            SplitOrientation.Rows,
            LayoutNodeDefinition.ForPanel(panelIds[0]),
            BuildVerticalStack(panelIds.Skip(1).ToList()),
            ratio: firstShare);
    }

    private static void ValidateRequest(ProjectTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("A project name is required.", nameof(request));
        }

        if (request.WorkerCount < 0 || request.WorkerCount > MaxWorkerCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                $"Worker count must be between 0 and {MaxWorkerCount}.");
        }
    }
}
