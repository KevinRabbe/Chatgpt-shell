namespace ChatGPTShell.Panels;

public sealed class ChatPanelDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string Title { get; set; } = "ChatGPT";

    public string ConversationUrl { get; set; } = "https://chatgpt.com/";

    public string? RoleId { get; set; }

    public string? RoleVersion { get; set; }

    public string? Specialization { get; set; }
}
