---
name: "suite-verification"
description: "Generates test cases, runs automated checks, and outputs system quality logs."
allowed-tools:
  - shell
---

# Suite Verification

Use this skill to generate tests, verify edge behaviors, and produce a formal quality record.

## Required Checks

- Map executed checks back to the active requirements and acceptance criteria, not just to compilation success.
- When frontend pages or routes are part of the change, explicitly verify route registration, user-visible entry points, redirect behavior, and preserved route accessibility.
- When backend runtime or developer-entry behavior is part of the change, explicitly verify the startup command, effective environment/profile, working-directory sensitivity, and reachability of documented local surfaces such as Swagger, health endpoints, or root redirects.
- If UI reachability was not validated directly, state that gap clearly in `docs/verification-report.md` instead of implying full feature verification.
- If backend startup or environment-sensitive behavior was not validated directly, state that gap clearly in `docs/verification-report.md` instead of implying full runtime verification.

## Output Format
Write outcomes to `docs/verification-report.md`:

```markdown
# Test Execution & Output Quality Report

## 1. Automated Test Execution Results
- **Unit Tests Execution:** [Passed/Failed] (X tests executed, 0 failures)
- **Integration Profile:** All service boundary links validated.

## 2. Documentation Quality Audit
- `docs/requirements.md`: Validated
- `docs/architecture.md`: Validated
- `docs/design-review.md`: Validated