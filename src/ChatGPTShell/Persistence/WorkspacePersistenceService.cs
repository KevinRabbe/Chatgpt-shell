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
            return WorkspaceDefinition.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(_workspacePath);
            var workspace = await JsonSerializer.DeserializeAsync<WorkspaceDefinition>(stream, JsonOptions);

            if (workspace is null || workspace.Panels.Count == 0)
            {
                return WorkspaceDefinition.CreateDefault();
            }

            return workspace;
        }
        catch (JsonException)
        {
            PreserveInvalidWorkspace();
            return WorkspaceDefinition.CreateDefault();
        }
    }

    public async Task SaveAsync(WorkspaceDefinition workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var tempPath = _workspacePath + ".tmp";

        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, workspace, JsonOptions);
            await stream.FlushAsync();
        }

        File.Move(tempPath, _workspacePath, overwrite: true);
    }

    private void PreserveInvalidWorkspace()
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var backupPath = $"{_workspacePath}.invalid.{timestamp}";
        File.Move(_workspacePath, backupPath, overwrite: false);
    }
}
