using System.IO;
using System.Text.Json;

namespace ChatGPTShell.Roles;

public sealed class RoleLibraryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _roleLibraryPath;
    private readonly string _defaultRoleLibraryPath;
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    public RoleLibraryService()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ChatGPTShell");

        Directory.CreateDirectory(dataDirectory);

        _roleLibraryPath = Path.Combine(dataDirectory, "roles.json");
        _defaultRoleLibraryPath = Path.Combine(AppContext.BaseDirectory, "Defaults", "roles.json");
    }

    public async Task<RoleLibraryDocument> LoadAsync()
    {
        if (!File.Exists(_roleLibraryPath))
        {
            return await RestoreDefaultsAsync();
        }

        try
        {
            var document = await ReadAsync(_roleLibraryPath);
            Validate(document);
            return document;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException)
        {
            PreserveInvalidLibrary();
            return await RestoreDefaultsAsync();
        }
    }

    public async Task SaveAsync(RoleLibraryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        Validate(document);

        await _saveGate.WaitAsync();

        try
        {
            var tempPath = _roleLibraryPath + ".tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, document, JsonOptions);
                await stream.FlushAsync();
            }

            File.Move(tempPath, _roleLibraryPath, overwrite: true);
        }
        finally
        {
            _saveGate.Release();
        }
    }

    public async Task<RoleDefinition> GetByIdAsync(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("A role id is required.", nameof(roleId));
        }

        var document = await LoadAsync();
        return document.Roles.SingleOrDefault(
                role => role.Id.Equals(roleId.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Role '{roleId}' was not found.");
    }

    private async Task<RoleLibraryDocument> RestoreDefaultsAsync()
    {
        if (!File.Exists(_defaultRoleLibraryPath))
        {
            throw new FileNotFoundException(
                "The default role library was not packaged with ChatGPT Shell.",
                _defaultRoleLibraryPath);
        }

        var document = await ReadAsync(_defaultRoleLibraryPath);
        Validate(document);
        await SaveAsync(document);
        return document;
    }

    private static async Task<RoleLibraryDocument> ReadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<RoleLibraryDocument>(stream, JsonOptions)
            ?? throw new InvalidDataException("The role library is empty.");
    }

    private static void Validate(RoleLibraryDocument document)
    {
        if (document.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported role-library schema version '{document.SchemaVersion}'.");
        }

        if (document.Roles is not { Count: > 0 })
        {
            throw new InvalidDataException("The role library must contain at least one role.");
        }

        var roleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in document.Roles)
        {
            if (role is null
                || string.IsNullOrWhiteSpace(role.Id)
                || string.IsNullOrWhiteSpace(role.Name)
                || string.IsNullOrWhiteSpace(role.Version)
                || string.IsNullOrWhiteSpace(role.PromptTemplate))
            {
                throw new InvalidDataException("Every role requires an id, name, version, and prompt template.");
            }

            if (!roleIds.Add(role.Id.Trim()))
            {
                throw new InvalidDataException($"Duplicate role id '{role.Id}'.");
            }

            try
            {
                _ = PromptComposer.Compose(role, new PromptCompositionContext());
            }
            catch (InvalidOperationException exception)
            {
                throw new InvalidDataException($"Role '{role.Id}' has an invalid prompt template.", exception);
            }
        }
    }

    private void PreserveInvalidLibrary()
    {
        if (!File.Exists(_roleLibraryPath))
        {
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff");
        var backupPath = $"{_roleLibraryPath}.invalid.{timestamp}";
        File.Move(_roleLibraryPath, backupPath, overwrite: false);
    }
}
