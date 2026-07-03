---
name: "peer-code-review"
description: "Runs a multi-point code quality check across security, DRY principles, test coverage, and dependency structures."
allowed-tools:
  - shell
---

# Peer Code Review

Use this skill to check code structures before pushing to remote repositories.

## Audit Matrix
Evaluates current workspace changes against these explicit gates:
- **Correctness:** Does code behave as specified in `docs/requirements.md`?
- **Route Reachability:** If frontend routes or pages changed, can the intended user actually discover and reach the feature through navigation, redirects, links, or the documented entry path?
- **Runtime Reachability:** If backend startup or developer-entry behavior changed, does the documented local run path actually resolve to the expected environment/profile and expose the documented surfaces?
- **Security:** Are credentials out of the source tree? Is user input fully escaped/validated?
- **Error Handling:** Are API edge drops, missing files, and unmapped collections handled?
- **Test Coverage:** Are both happy paths and missing-field/not-found paths handled?
- **Code Clarity:** Are function mappings cleanly structured?
- **DRY Principle:** Is there logic coupling that can be extracted into shared utilities?
- **Dependency Safety:** Are vulnerable packages or configuration mismatches caught?

## Output Format
Write review results cleanly into `docs/code-review.md`:

```markdown
# Peer Review Audit Report

| Metric | Pass/Fail | Findings / Required Refactors | Action Taken |
|:---|:---:|:---|:---|
| **Correctness** | PASS | Matches `docs/requirements.md` expectations. | None |
| **Route Reachability** | PASS | Required route is registered and exposed through the intended UI entry points. | None |
| **Runtime Reachability** | PASS | Local startup path resolves to the expected environment/profile and exposes documented developer surfaces. | None |
| **Security** | FAIL | Found raw string inputs unvalidated in asset routes. | Patched middleware |