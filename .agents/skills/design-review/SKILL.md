---
name: "design-review"
description: "Performs an algorithmic system design review on an architecture specification. Use this skill when asked to review risks, track design decisions, or log architecture anti-patterns inside the docs directory."
allowed-tools:
  - shell
metadata:
  github-path: skills/design-review
  version: 1.0.1
---

# Design Review

Use this skill to systematically find technical debt, concurrency risks, and security gaps in `docs/architecture.md` and log the mitigation patterns into a formal review ledger located at `docs/design-review.md`.

## Core Scope

1. **Input Baseline**: Expects a local `architecture.md` file within the `docs/` folder (`docs/architecture.md`).
2. **Audit Metrics**: Validates data durability, service decoupling, error-handling topologies, and compliance with performance limits.
3. **Mutation Mapping**: Tracks all architectural changes resulting from review inputs and logs them in the same directory.

## Workflow

1. **Scan Target**: Read the structural components and Mermaid topology from `docs/architecture.md`.
2. **Audit Check**: Evaluate against common failures (e.g., untrusted entry points, slow database joins, network coupling).
3. **Write Decision Log**: Compile findings and write them directly into `docs/design-review.md`.

## Output Format

The review ledger must always follow this structured template when outputted to `docs/design-review.md`:

```markdown
# Design Review Ledger: [KAN Project Subsystem]

## 1. Architectural Risk & Gap Register
| Risk ID | Component Affected | Severity | Vulnerability Description | Mitigation Strategy |
|:---|:---|:---|:---|:---|
| **RSK-01** | API Gateway / Contacts | High | Single point of failure during auth mapping | Introduce Redis cache fallback layer |
| **RSK-02** | Asset Worker | Medium | Sync HTTP call creates network coupling | Transition to event-driven message broker |

## 2. Architecture Decision Records (ADR)
### ADR-001: [Short Title of Decision]
* **Status:** [Proposed | Accepted | Rejected]
* **Context:** [What technical reality forced this decision]
* **Decision:** [The concrete architectural path chosen]
* **Consequences:** [What we gain or lose by committing to this path]

## 3. Post-Review Action Items
- [ ] Implement mitigation for RSK-01 in `docs/architecture.md`
- [ ] Patch component layout map to reflect updated event-stream channels