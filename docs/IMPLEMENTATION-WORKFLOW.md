# Implementation Workflow

## Phase-by-Phase Workflow

For each implementation phase, the following process will be followed:

```
┌─────────────────────────────────────────────────────────────────────────┐
│  PHASE X WORKFLOW                                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  1. CREATE IMPLEMENTATION PLAN                                          │
│     ├── Detailed, step-by-step instructions                             │
│     ├── Specific file paths and code changes                            │
│     ├── Expected outcomes for each step                                 │
│     └── Skill level: Claude Haiku 4.5 / Gemini Flash 3.0 / equivalent   │
│                                                                         │
│  2. VALIDATE PLAN                                                       │
│     └── Human review and approval (mandatory; AI must stop here)        │
│                                                                         │
│  3. EXECUTE WITH DEDICATED AGENT                                        │
│     ├── AI agent follows the plan                                       │
│     └── Creates implementation commit                                   │
│                                                                         │
│  4. VERIFY & FIX                                                        │
│     ├── Run tests: dotnet test                                          │
│     ├── Build verification: dotnet build                                │
│     ├── Review code quality                                             │
│     └── Mixed AI/human effort for bug fixes                             │
│                                                                         │
│  5. DOCUMENTATION UPDATE                                                │
│     ├── Update feasibility doc if needed                                │
│     ├── Note any gaps for future phases                                 │
│     └── Update migration guide if applicable                            │
│                                                                         │
│  6. ITERATE TO PHASE X+1                                                │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

## Implementation Plan Requirements

Each phase plan must include:

1. **Objective**: Clear statement of what the phase accomplishes
2. **Prerequisites**: What must be complete before starting
3. **Step-by-Step Instructions**:
   - Numbered steps with specific actions
   - Exact file paths (absolute or relative to project root)
   - Code snippets or templates where helpful
   - Expected state after each step
4. **Verification Steps**: How to confirm the phase is complete
5. **Rollback Instructions**: How to undo if something goes wrong

## Mandatory Approval Gate

After the plan is written, human review and approval is required before any implementation.
AI agents must not continue past plan validation without explicit approval.
