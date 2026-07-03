---
name: "implementation-planning"
description: "Deconstructs an architectural document into a prioritized, dependency-ordered task breakdown file."
allowed-tools:
  - shell
---

# Implementation Planning

Use this skill to calculate task dependencies, optimize the order of backend/frontend development, and generate a clear milestone blueprint in `docs/impl-plan.md`.

## Planning Guardrails

- Include explicit tasks for preserved behavior, negative-path handling, and verification traceability when those expectations appear in `docs/requirements.md`.
- If frontend routes or pages are in scope, include tasks for route registration, user-visible navigation entry points, redirect behavior, and preserved route accessibility.
- If backend services are in scope, include tasks for local startup configuration, launch profiles or equivalent environment setup, cwd-safe run commands, and validation of any environment-gated developer surfaces such as Swagger or health endpoints.

## Output Format
The tracking document must always use this structural format in `docs/impl-plan.md`:

```markdown
# Engineering Implementation Plan

## 1. Dependency Graph (Mermaid)
```mermaid
gantt
    title Implementation Timeline
    section Core Infrastructure
    Task 1 (Database) :active, cp1, 2026-07-01, 1d
    section Services
    Task 2 (API)      :cp2, after cp1, 1d