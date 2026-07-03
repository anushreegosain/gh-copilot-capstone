# Copilot Repository Hooks

This repository defines Copilot hooks in [.github/hooks/repo-hooks.json](./repo-hooks.json).

Configured events:
- `sessionStart`
- `preToolUse`
- `postToolUse`
- `sessionEnd`

## Simple custom hook included

A reusable custom logger hook is implemented in:
- PowerShell: [.github/hooks/scripts/log-hook-event.ps1](./scripts/log-hook-event.ps1)
- Bash: [.github/hooks/scripts/log-hook-event.sh](./scripts/log-hook-event.sh)

The hook reads JSON input from stdin and appends one JSON line per event to:
- `.github/hooks/logs/agent-hooks.jsonl`

## Notes

- Hook config must be committed to the default branch to run in GitHub Copilot cloud agent.
- Keep hooks lightweight; they run synchronously and can block agent execution.
