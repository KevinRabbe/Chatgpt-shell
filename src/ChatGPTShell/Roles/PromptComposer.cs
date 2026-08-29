namespace ChatGPTShell.Roles;

public static class PromptComposer
{
    private static readonly IReadOnlyDictionary<string, Func<RoleDefinition, PromptCompositionContext, string>> Tokens =
        new Dictionary<string, Func<RoleDefinition, PromptCompositionContext, string>>(StringComparer.Ordinal)
        {
            ["{{ROLE_ID}}"] = (role, _) => role.Id,
            ["{{ROLE_NAME}}"] = (role, _) => role.Name,
            ["{{ROLE_VERSION}}"] = (role, _) => role.Version,
            ["{{PROJECT_NAME}}"] = (_, context) => ValueOrFallback(context.ProjectName, "Not specified"),
            ["{{TECH_STACK}}"] = (_, context) => ValueOrFallback(context.TechStack, "Not specified"),
            ["{{WORKFLOW}}"] = (_, context) => ValueOrFallback(context.Workflow, "Not specified"),
            ["{{SPECIALIZATION}}"] = (_, context) => ValueOrFallback(context.Specialization, "General"),
            ["{{PROJECT_CONTEXT}}"] = (_, context) => ValueOrFallback(context.ProjectContext, "No additional project context supplied.")
        };

    public static string Compose(RoleDefinition role, PromptCompositionContext context)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(context);

        if (string.IsNullOrWhiteSpace(role.PromptTemplate))
        {
            throw new InvalidOperationException($"Role '{role.Id}' does not define a prompt template.");
        }

        var prompt = role.PromptTemplate.Replace("\r\n", "\n", StringComparison.Ordinal);

        foreach (var token in Tokens)
        {
            prompt = prompt.Replace(token.Key, token.Value(role, context), StringComparison.Ordinal);
        }

        if (prompt.Contains("{{", StringComparison.Ordinal)
            || prompt.Contains("}}", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Role '{role.Id}' contains an unresolved prompt variable.");
        }

        return prompt.Trim();
    }

    private static string ValueOrFallback(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
