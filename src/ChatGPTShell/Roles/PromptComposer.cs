using System.Text.RegularExpressions;

namespace ChatGPTShell.Roles;

public static partial class PromptComposer
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
        ValidateTemplateVariables(role.Id, prompt);

        foreach (var token in Tokens)
        {
            prompt = prompt.Replace(token.Key, token.Value(role, context), StringComparison.Ordinal);
        }

        return prompt.Trim();
    }

    private static void ValidateTemplateVariables(string roleId, string promptTemplate)
    {
        foreach (Match match in PromptVariableRegex().Matches(promptTemplate))
        {
            if (!Tokens.ContainsKey(match.Value))
            {
                throw new InvalidOperationException(
                    $"Role '{roleId}' contains unsupported prompt variable '{match.Value}'.");
            }
        }
    }

    private static string ValueOrFallback(string value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    [GeneratedRegex(@"\{\{[A-Z0-9_]+\}\}", RegexOptions.CultureInvariant)]
    private static partial Regex PromptVariableRegex();
}
