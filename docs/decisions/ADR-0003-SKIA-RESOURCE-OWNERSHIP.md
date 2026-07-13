# ADR-0003: Skia resource ownership

- **Status:** accepted
- **Date:** 2026-07-13

## Context

The legacy implementation allocated `SKBitmap`, `SKCanvas`, `SKPaint`, `SKPath` and encoded `SKData` objects without consistently disposing them. These objects may own native memory, making garbage-collector timing insufficient for high-resolution rendering.

M1 contains no compiled Skia dependency. M2 will introduce the first Skia imaging adapter, and every adapter must follow an ownership contract before it can be merged.

## Decision

1. Domain and Application never expose Skia types.
2. A method that creates a Skia object owns and disposes it unless ownership is explicitly transferred by its return type.
3. Temporary `SKBitmap`, `SKImage`, `SKSurface`, `SKCanvas`, `SKPaint`, `SKPath`, `SKData` and streams use `using` declarations or an equivalent deterministic lifetime.
4. Returned native resources must be wrapped in an adapter whose public contract documents disposal ownership.
5. Cached native resources require a single owning service implementing `IDisposable` or `IAsyncDisposable`.
6. Tests must exercise repeated load/render/dispose cycles and verify that disposed wrappers reject further use where practical.
7. Final rendering must not rely on finalizers to release peak-memory buffers.

## Consequences

- Native concerns remain isolated from the generative plans.
- Resource lifetime becomes reviewable at adapter boundaries.
- Some image operations may copy managed data to avoid leaking native ownership across layers.
- M1 satisfies the ownership decision by introducing no native allocations in the migrated planner; implementation validation begins when M2 adds Skia.
