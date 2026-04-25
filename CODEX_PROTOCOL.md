# CODEX_PROTOCOL.md — ARCONTIO AI Workflow Protocol

This file defines the permanent workflow discipline for Codex operations inside ARCONTIO.

---

# 1. Branch workflow discipline

Stable branch:
`main`

AI integration branch:
`ai/codex-main`

Temporary Codex task branches:
`ai-task/v0.xx.yy-short-description`

Lifecycle:

1. read task context
2. inspect involved files
3. patch on temporary ai-task branch
4. show diff
5. create PR into ai/codex-main
6. operator reviews/merges
7. temporary branch can be deleted

Codex should not accumulate historical long-lived task branches.

---

# 2. Mandatory pre-read files before implementation

Codex must inspect:

- CODEX_CONTEXT.md
- TASKBOARD_CODEX.md
- REPO_MAP.md

before starting any non-trivial implementation.

If task is architectural, audit-first mode is mandatory.

---

# 3. Patch discipline

Codex must prefer:

- smallest viable safe patch,
- minimum touched files,
- no opportunistic cleanup,
- no encoding rewrites,
- no formatting churn,
- no line ending churn,
- no comment rewriting unless necessary.

When moving code blocks, preserve original comments exactly when possible.

---

# 4. Audit-first rule

For these tasks Codex should audit before coding:

- runtime architecture
- scheduler ordering
- pathfinding
- memory systems
- job systems
- AI decision systems
- save/load
- world contracts
- telemetry/explainability

Audit output should identify:

- files,
- current contracts,
- coupling points,
- safest patch candidate.

Only then patch.

---

# 5. Output reporting contract

After each implementation Codex must report:

## Modified files
exact list

## What changed
surgical summary

## Runtime behavior impact
what changes / what is preserved

## Risks
possible side effects

## Git readiness
diff / PR status

---

# 6. Documentation awareness

ARCONTIO_docs is the external project memory repository.

When implementation affects architecture or project conventions,
Codex should explicitly notify:

"ARCONTIO_docs alignment recommended."