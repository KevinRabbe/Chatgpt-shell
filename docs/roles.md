# Roles and prompt composition

## Purpose

Roles describe how a ChatGPT conversation should operate inside a project workspace. They are data, not application behavior.

The shell ships six starter roles:

- `manager` — project planning, priorities, dependencies, acceptance criteria, and handoffs;
- `worker` — scoped implementation and validation;
- `architect` — technical boundaries, contracts, scalability, and migration design;
- `reviewer` — independent verification and regression review;
- `researcher` — evidence gathering and option analysis;
- `general` — flexible project conversation.

The canonical shipped definitions live in `src/ChatGPTShell/Defaults/roles.json`.

On first use, the role library is copied to `%LOCALAPPDATA%/ChatGPTShell/roles.json`. That local file is the editable working library. Invalid local edits are preserved with an `.invalid.<timestamp>` suffix before the shipped defaults are restored.

## Versioned identity

A saved `ChatPanelDefinition` may record:

```text
roleId
roleVersion
specialization
```

The role id selects the reusable role. The recorded version identifies the prompt contract that was selected for that panel. Specialization narrows a reusable role without duplicating the entire role definition, for example `worker` + `World generation`.

## Prompt variables

`PromptComposer` resolves these variables deterministically:

```text
{{ROLE_ID}}
{{ROLE_NAME}}
{{ROLE_VERSION}}
{{PROJECT_NAME}}
{{TECH_STACK}}
{{WORKFLOW}}
{{SPECIALIZATION}}
{{PROJECT_CONTEXT}}
```

Unknown or unresolved variables make the role definition invalid instead of silently producing a partial bootstrap prompt.

The current composer does not send prompts into the ChatGPT page. Prompt generation and prompt delivery are intentionally separate boundaries so the harness does not depend on scraping or automating ChatGPT's internal DOM.

## Validation

The local role library must:

- use schema version `1`;
- contain at least one role;
- use unique role ids, case-insensitively;
- give every role an id, name, version, and prompt template;
- resolve every prompt variable using the supported composition contract.

The library is validated and seeded during application startup.
