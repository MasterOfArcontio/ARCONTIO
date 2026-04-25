<!-- UNITY CODE ASSIST INSTRUCTIONS START -->
- Project name: Arcontio
- Unity version: Unity 6000.3.3f1
<!-- UNITY CODE ASSIST INSTRUCTIONS END -->

# AGENTS.md — ARCONTIO Codex Entry Instructions

This repository is operated under strict AI patch discipline.

Before any coding activity, Codex must read:

- CODEX_CONTEXT.md
- CODEX_PROTOCOL.md
- TASKBOARD_CODEX.md
- REPO_MAP.md

These files define repository architecture, workflow rules, and current active task state.

---

## Repository role

This repository (`ARCONTIO`) contains Unity production code only.

Project planning, engineering logs, constitutional architecture notes, nomenclature,
and long-form development memory are maintained in the separate repository:

`ARCONTIO_docs`

Codex does not modify documentation repository unless explicitly requested.

---

## Critical branch policy

Protected stable branch:

`main`

AI integration base branch:

`ai/codex-main`

Codex must NEVER perform implementation work directly on `main`.

For every implementation/audit task Codex must create or operate on a temporary task branch named:

`ai-task/v0.xx.yy-short-description`

unless the operator explicitly requests a different branch.

All pull requests must target:

`ai/codex-main`

never `main`, unless explicitly instructed.

---

## Codex operating philosophy

ARCONTIO is a deterministic simulation architecture with fragile runtime coupling.

Because of this:

- no blind feature coding,
- no broad refactors without audit,
- no multi-file expansion unless required,
- no unrelated cleanup during implementation tasks.

Architectural tasks must begin with read-only inspection of involved contracts and files.

Codex should prefer:

smallest safe patch,
single-responsibility edits,
behavior-preserving modifications first.

---

## Required output after every task

Codex must always report:

1. touched files,
2. exact behavioral impact,
3. exact diff summary,
4. risks / follow-up notes,
5. PR readiness status.

Do not silently patch and stop.

---

## Documentation synchronization reminder

If a patch changes:

- architecture contracts,
- simulation behavior,
- workflow policy,
- repository structure,

Codex must remind the operator that `ARCONTIO_docs` may require alignment.