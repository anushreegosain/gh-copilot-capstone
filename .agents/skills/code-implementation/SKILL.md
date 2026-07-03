---
name: "code-implementation"
description: "Executes programmatic code generation, module creation, and code file patches based on the active ticket requirements. Use this whenever implementation work must stay tightly aligned to docs/requirements.md, docs/architecture.md, and docs/impl-plan.md, especially when preserving existing behavior, handling error paths, or matching user-visible validation and messaging is important."
allowed-tools:
  - shell
---

# Code Implementation

Use this skill to implement the active ticket from the current planning artifacts, not from older examples or historical ticket assumptions.

## Required Inputs

Before writing code, read and align to:
- `docs/requirements.md`
- `docs/architecture.md`
- `docs/impl-plan.md`
- `docs/design-review.md` when present

## Workflow

1. Build a short checklist from the current requirements covering:
   - required feature behavior
   - preserved existing behavior
  - route registration, navigation entry points, redirect behavior, and user-reachable exposure when frontend pages or routes are affected
  - local startup expectations, launch-profile/runtime configuration, and environment-sensitive behavior when backend services are affected
   - negative/error paths
   - user-visible validation and error messages
   - required tests
2. Implement the smallest unblocked slice from the plan.
3. Validate that slice immediately with the narrowest relevant check.
4. Update or add focused tests when the changed behavior is not already protected.
5. Repeat until each checklist item is either implemented and verified or explicitly blocked.

## Guardrails

- Do not copy ticket-specific implementation details from prior work unless they are present in the current docs.
- Do not replace existing working flows with placeholders if the current requirements say they must be preserved.
- Do not treat compile success as sufficient for stories that include runtime behavior, redirects, or displayed messages.
- When account creation, sign-in, onboarding, or form handling is in scope, verify the actual user-facing failure messages and validation states.
- When adding or changing frontend routes, do not stop at route registration. Also wire the expected entry point through navigation, redirects, call-to-action surfaces, or other requirement-defined access paths so the feature is visible and reachable.
- When adding or changing backend startup behavior, do not stop at build success. Verify the intended local run command, launch profile or equivalent environment configuration, and any environment-gated endpoints or docs claims that depend on that startup path.
- If a review or verification step finds a miss, repair the miss and rerun the smallest affected validation before moving on.

## Completion Standard

Implementation is complete only when the changed slice is:
- code-complete
- behaviorally validated
- covered by focused tests where practical
- consistent with the active requirement checklist