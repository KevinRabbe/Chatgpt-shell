using System.IO;
using System.Text.Json;
using ChatGPTShell.Workspaces;

namespace ChatGPTShell.Persistence;

public sealed class WorkspacePersistenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _workspacePath;
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    public WorkspacePersistenceService()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTShell");

        Directory.CreateDirectory(dataDirectory);
        _workspacePath = Path.Combine(dataDirectory, "workspace.json");
    }

    public async Task<WorkspaceDefinition> LoadOrCreateAsync()
    {
        if (!File.Exists(_workspacePath))
        {
            var newWorkspace = WorkspaceDefinition.CreateDefault();
            await SaveAsync(newWorkspace);
            return newWorkspace;
        }

        WorkspaceDefinition? workspace = null;

        try
        {
            await using var stream = File.OpenRead(_workspacePath);
            workspace = await JsonSerializer.DeserializeAsync<WorkspaceDefinition>(stream, JsonOptions);
        }
        catch (JsonException)
        {
            // Preserve the original file below and replace it with a usable default.
        }

        if (workspace?.Panels is { Count: > 0 } panels
            && panels.All(panel => panel is not null))
        {
            return workspace;
        }

        PreserveInvalidWorkspace();

        var replacement = WorkspaceDefinition.CreateDefault();
        await SaveAsync(replacement);
        return replacement;
    }

    public async Task SaveAsync(WorkspaceDefinition workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        await _saveGate.WaitAsync();

        try
        {
            var tempPath = _workspacePath + ".tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, workspace, JsonOptions);
                await stream.FlushAsync();
            }

            File.Move(tempPath, _workspacePath, overwrite: true);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private void PreserveInvalidWorkspace()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var backupPath = $"{_workspacePath}.invalid.{timestamp}";
        File.Move(_workspacePath, backupPath, overwrite: false);
    }
}
