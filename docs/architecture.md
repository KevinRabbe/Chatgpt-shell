# Architecture

## Product boundary

ChatGPT Shell is a project-oriented harness around ChatGPT, not a general-purpose browser.

The application owns project/workspace structure, panel state, prompts, layouts, and persistence. ChatGPT owns the conversation experience. WebView2 is an implementation detail used to host that experience.

## Resource rule

**Store lots. Run little.**

Saved chats and project metadata should be cheap. Live ChatGPT surfaces are comparatively expensive and should be created only when needed and disposable when inactive.

## v0.1 architecture

```text
App
├─ shared WebViewEnvironmentService
├─ WorkspacePersistenceService
└─ MainWindow
   └─ WorkspaceHost
      └─ recursive LayoutNodeDefinition tree
         └─ ChatPanel leaves
            ├─ ChatPanelDefinition
            └─ WebView2
```

### Shared WebView environment

All chat panels use one `CoreWebView2Environment` and one persistent user-data directory under `%LOCALAPPDATA%/ChatGPTShell/WebView2`.

This gives panels a common authentication/session profile and prevents each panel from inventing its own browser profile.

### Panel definition vs. runtime surface

`ChatPanelDefinition` is persistent-friendly state: identity, title, role, and conversation URL.

`ChatPanel` is the live UI/WebView surface. Later versions may unload a `ChatPanel` while retaining its definition.

This separation is mandatory for scaling to projects with many saved chats but only a small number of live WebViews.

### Workspace persistence

`WorkspaceDefinition` owns the saved panel collection and the currently active panel identity.

Workspace state is stored as human-readable JSON at `%LOCALAPPDATA%/ChatGPTShell/workspace.json`.

Writes use a temporary file followed by replacement so an interrupted save does not partially overwrite the last valid workspace. Saves are serialized through one gate to prevent concurrent navigation events from racing the same file.

Malformed or structurally unusable workspace JSON is preserved with an `.invalid.<timestamp>` suffix before a default workspace is created.

A `ChatPanel` reports persistent state changes through `DefinitionChanged`; it does not write files itself. This keeps the live WebView surface disposable and leaves persistence ownership above the panel layer.

### Recursive layout tree

The layout system deliberately has only two node kinds:

```text
Panel(panelId)
Split(orientation, ratio, first, second)
```

`WorkspaceHost` recursively converts this tree into WPF grids. A panel leaf creates one live `ChatPanel`; a split creates two child regions separated by a `GridSplitter`.

There are no special `TwoColumns`, `ThreeColumns`, or `FourGrid` application modes. Any number of live panels can be represented by composing the same two primitives.

Saved panel definitions that do not appear in the layout tree are dormant and create no WebView. A valid layout may reference a saved panel at most once.

Splitter ratios are persisted on drag completion and clamped to a usable range. Older workspaces without a layout tree automatically migrate to a one-panel tree rooted at the active saved panel.

## Explicit non-goals

The shell does not aim to provide:

- an address bar;
- bookmarks;
- arbitrary web browsing;
- browser extensions;
- private/incognito browsing;
- a general tab ecosystem;
- a download manager;
- browser history management.

## Planned layering

```text
Project
├─ Workspace
│  ├─ Panel definitions
│  └─ Layout tree
├─ Role definitions
├─ Prompt/context modules
└─ Persistence

Runtime
├─ WorkspaceHost
│  └─ active ChatPanel surfaces only
└─ shared WebView2 environment
```

Future features should preserve the same boundary: project/harness behavior belongs in the native shell; general browser behavior does not.
