# ADR 008: Vertical Slice Development

## Status
Accepted

## Context
Horizontal layer completion leads to big bang integrations. Features should be developed end-to-end.

## Decision
Develop features vertically through all layers rather than completing layers horizontally.

## Consequences
- ✅ Working software earlier
- ✅ Better feedback loops
- ✅ Easier to pivot
- ⚠️ Some code duplication initially

## Alternatives Considered
- Layer-by-layer development (delayed integration)
- Big design upfront (analysis paralysis)
- Prototyping only (technical debt)

## References
- Vertical slice architecture
