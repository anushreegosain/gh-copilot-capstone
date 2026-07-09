---
description: Create reliable Playwright UI automation tests for web apps. Use this skill whenever the user wants browser-based end-to-end tests, selectors, assertions, regression coverage, or Playwright MCP-driven test authoring.
name: playwright-ui-automation
---

# Playwright UI Automation

Use this skill to turn product flows into reliable end-to-end browser tests with Playwright.

## When to use this skill
Use it when the user asks for any of the following:
- Create or update Playwright tests
- Add UI automation coverage for a feature or regression
- Inspect a page and generate selectors or test steps
- Improve flaky or brittle browser tests
- Build tests for login, registration, forms, validation, or navigation flows

## Core workflow
1. Understand the target user journey and expected outcome.
2. Inspect the application UI to identify the relevant page, controls, and states.
3. Prefer stable locators such as role-based, label-based, and test-id-based selectors.
4. Write concise Playwright tests with clear naming, meaningful assertions, and minimal duplication.
5. Run the tests, fix failures, and tighten selectors or waits if needed.
6. Report the created or updated tests, plus any commands needed to run them.

## Guidance
- Favor Playwright's built-in auto-waiting over manual sleeps.
- Prefer resilient selectors like `getByRole`, `getByLabel`, `getByText`, and `getByTestId`.
- Avoid brittle selectors based on CSS indexes or overly specific DOM paths.
- Keep each test focused on a single user journey.
- Use meaningful assertions that verify user-visible behavior.
- If auth or state is needed, use fixtures or storage state rather than hardcoded secrets.

## Output expectations
When this skill is used, produce one of the following:
- A Playwright test file or patch
- A list of recommended selectors and test steps
- A small set of test cases for the requested flow
- A summary of issues found while validating the flow, including any needed follow-up

## Quality checklist
Before finishing, verify that:
- The test targets a real user action or visible outcome
- The selectors are stable and maintainable
- The assertions match the intended behavior
- The test can be run locally with a clear command
- Any assumptions or environment requirements are clearly stated

## Example prompts
- Create a Playwright test for the login flow.
- Add UI automation coverage for the registration form.
- Inspect this page and generate a stable Playwright test for the main action.
- Write a regression test for a validation error state.
