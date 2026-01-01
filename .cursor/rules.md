# Cursor Rules — SaveState Reborn

## Absolute Constraints

- Result<T> is mandatory. No null returns.
- No business exceptions.
- IHttpClientFactory only.
- Pagination required for all list queries.
- Follow Clean Architecture and CQRS strictly.

## Authority Order

1. docs/ENGINEERING_RULES.md (non-negotiable)
2. docs/AI_MASTER_CONTEXT.md (canonical patterns)
3. docs/AI_PROJECT_INDEX.md (navigation & conflict resolution)

## Behavior

- Match existing patterns before proposing changes.
- Ask before refactoring core or hotspot files.
- When unsure, retrieve relevant docs instead of guessing.
