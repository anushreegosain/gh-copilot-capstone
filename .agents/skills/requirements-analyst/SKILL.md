---
name: requirements-analyst
description: Analyze user stories and requirements from Jira, clarify scope gaps interactively, and generate comprehensive local documentation. Use this skill when analyzing requirements, clarifying feature scope, extracting requirements from Jira issues, resolving ambiguities, or generating requirements documentation.
compatibility:
  - Jira MCP (for reading stories from KAN board)
  - File system tools (for creating documentation)
---

# Requirements Analyst Skill

This skill enables systematic analysis of requirements from Jira user stories, identification of scope gaps, interactive clarification of ambiguities, and generation of local documentation.

## When to Use This Skill

Use this skill whenever you need to:
- **Analyze** user stories or requirements from the KAN Jira board
- **Clarify** scope gaps or ambiguous requirements
- **Document** requirements comprehensively for implementation
- **Resolve** unclear acceptance criteria or feature boundaries
- **Extract** structured requirements for planning or development

## Workflow

### 1. Fetch and Analyze Jira Stories

Start by retrieving the relevant issues from the KAN Jira board:

```
Tools to use:
- mcp_atlassian-mcp_searchJiraIssuesUsingJql
- mcp_atlassian-mcp_getJiraIssue
```

**JQL Query patterns:**
- `project = KAN AND type = Story` — all stories
- `project = KAN AND type = Epic` — epics
- `project = KAN AND status = "To Do"` — backlog items
- `project = KAN AND assignee = currentUser()` — assigned to you
- `project = KAN AND key = KAN-123` — specific issue

**When fetching issues:**
- Include `comment` field to read discussion/clarifications
- Fetch full details including custom fields
- Extract: summary, description, acceptance criteria, story points

### 2. Identify Scope Gaps

Analyze each story for:

- **Unclear acceptance criteria** — Are there multiple interpretations?
- **Missing technical details** — Edge cases, error handling, performance requirements?
- **Undefined boundaries** — What's in/out of scope?
- **Ambiguous terminology** — Domain-specific terms that need definition?
- **Missing dependencies** — Does this require other stories or systems?
- **Incomplete requirements** — Are there user flows, data structures, or API contracts undefined?

### 3. Interactive Clarification

For each gap identified:

1. **Ask specific questions** to the user to clarify:
   - What does "X" mean in this context?
   - Should we handle scenario Y?
   - Is Z a blocker or a nice-to-have?

2. **Propose solutions** based on org standards and document them in memory for consistency

3. **Record decisions** in session memory as they're clarified

### 4. Generate Requirements Documentation

Create a structured markdown document with these sections:

```markdown
# [Feature Name]

## Overview
Brief summary of what's being built and why.

## Story Details
- **Jira Key:** KAN-XXX
- **Story Points:** X
- **Status:** Draft/Active

## Requirements

### Functional Requirements
- Clear, numbered list of what the system must do
- Each requirement should be testable

### Non-Functional Requirements
- Performance requirements
- Security requirements
- Scalability requirements

## Acceptance Criteria
- [ ] Criterion 1: Testable statement
- [ ] Criterion 2: Testable statement

## Clarifications & Scope Decisions
- **Gap:** Original concern
- **Clarification:** What was decided
- **Rationale:** Why this decision was made

## Dependencies
- Related stories: KAN-XXX, KAN-YYY
- External dependencies: System X, API Y

## Implementation Notes
- Technical approach (if determined)
- Considerations for developers
- Known limitations or trade-offs

## Open Questions
- Any remaining questions for stakeholder
```

### 5. Store and Track

- Save requirements documentation to `/docs` or `/requirements` folder
- Use consistent naming: `KAN-XXX-requirements.md`
- Track clarifications in session memory for future reference
- Update Jira issue with links to documentation if applicable

## Key Principles

**Be Thorough:** Don't settle for ambiguous requirements. Good questions now prevent costly rework later.

**Be Specific:** Avoid vague language like "user-friendly" or "fast". Quantify and measure.

**Organize by Perspective:** Consider the perspective of implementers (developers), testers, and users.

**Document Decisions:** Every clarification or assumption should be documented with the rationale.

**Reference Standards:** When clarifying, reference org standards (naming conventions, API patterns, etc.) from copilot-instructions.md.

## Example Interaction

**Story:** "As a user, I want to export data"

**Gaps Identified:**
1. What formats are supported? (CSV, JSON, Excel?)
2. What data fields are included?
3. Are there size/performance limits?
4. Is this feature available to all users or specific roles?

**Clarifications Asked:**
- Format: CSV, JSON, Excel (user selectable)
- Data: All non-sensitive fields visible in the UI
- Limit: Max 100k rows, return error if exceeded
- Access: All authenticated users

**Documentation Created:** `/docs/export-feature-requirements.md` with all details, rationale, and acceptance criteria

## Tools Available

- **Jira Search:** `mcp_atlassian-mcp_searchJiraIssuesUsingJql`
- **Fetch Issue:** `mcp_atlassian-mcp_getJiraIssue`
- **Add Comment:** `mcp_atlassian-mcp_addCommentToJiraIssue`
- **Create Issues:** `mcp_atlassian-mcp_createJiraIssue` (for creating clarification subtasks)

## Output Artifacts

When done, deliver:
1. **Requirements Document** — Markdown file with complete analysis
2. **Scope Summary** — List of what's in/out of scope
3. **Clarification Log** — Record of gaps identified and how they were resolved
4. **Next Steps** — Clear action items for implementation team
