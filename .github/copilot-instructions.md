# GitHub Copilot Workspace Instructions

## Project type
- Unity 2022.3.48f1 game project.
- Primary source assets live under `Assets/`.
- Typical workflow uses the Unity Editor and the generated Visual Studio/Rider solution files.

## Key files and directories
- `Assets/` — main game content, including C# scripts, scenes, prefabs, audio, materials.
- `ProjectSettings/` — Unity project configuration.
- `Packages/manifest.json` — package dependencies and editor/runtime packages.
- `Tower Defence.sln` and `Assembly-CSharp.csproj` — IDE entry points for script editing.
- `Library/`, `Temp/` — generated Unity caches; do not edit or treat as source.

## Build and run guidance
- Prefer Unity Editor for builds and testing.
- Open the project from `Tower Defence.sln`, or launch Unity on the repository root.
- There is no root `README.md` or existing agent bootstrap file in this repo.
- Use the Unity Test Framework package if test-related work is requested, but verify whether tests exist before creating them.

## Coding conventions and boundaries
- Focus changes on `Assets/` and relevant `ProjectSettings/` entries.
- Avoid editing generated or package cache content under `Library/` and `Packages/` unless the package manifest explicitly requires it.
- When adding or updating scripts, keep Unity-specific lifecycle methods and serialization conventions in mind.
- Prefer non-destructive edits: preserve existing scene and prefab references when modifying components.

## What the agent should do first
- Identify the requested task within Unity project context.
- Locate relevant `Assets/` C# script files or scene assets.
- Avoid making assumptions based on non-Unity repo conventions.

## What the agent should not do
- Do not modify binary assets or metadata without a clear need.
- Do not rely on external build tools outside Unity unless the user asks explicitly.
- Do not use `Library/` or `Temp/` contents as source of truth.

## Example prompts for this workspace
- "Inspect `Assets/Scripts/` and fix the enemy pathfinding logic."
- "Help me add a new tower type to the Unity scene and its C# behavior."
- "Review the Unity package manifest and suggest safe package changes."
