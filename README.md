# ChatGPT Shell

A deliberately minimal, project-oriented harness around ChatGPT.

## Core principle

**Store lots. Run little.**

The shell should spend as few resources as practical on itself. Saved project state is cheap; live ChatGPT surfaces are the expensive, disposable runtime component.

## Product boundary

ChatGPT Shell is **not** a general-purpose web browser. It does not aim to provide an address bar, bookmarks, arbitrary browsing, extensions, private mode, or a browser-style tab ecosystem.

Its job is to provide:

- persistent ChatGPT sessions;
- reusable chat panels;
- project/workspace layouts;
- role and prompt templates;
- lightweight project state and persistence;
- controlled loading/unloading of live ChatGPT surfaces.

The first milestone is intentionally small: one reusable ChatGPT panel hosted in a native Windows shell with persistent authentication.
