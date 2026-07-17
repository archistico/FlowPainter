# Next steps after M15.2

**Planning baseline:** M15.2 validated with 1,071 tests  
**Immediate next milestone:** M15.3 — Staged Flow rendering

## Why this order

The regional analysis stack is now complete enough to guide rendering: SLIC labels, descriptors, the RAG, hierarchical merge, regional roles, a continuous boundary field and local high-detail stroke geometry are all validated. The remaining work must therefore proceed from rendering orchestration to editing and only then to optimization.

The approved order is:

```text
M15.3 Staged Flow rendering
        ↓
M15.4 Primitive coarse-to-fine
        ↓
M15.5 Unified artistic hierarchy
        ↓
M16 Advanced regional editing
        ↓
M17 High-resolution optimization and release
```

Changing this order would create avoidable rework. Building advanced region editors before the rendering hierarchy is stable would persist concepts that may later change. Optimizing high-resolution execution before pass budgets and invalidation scopes exist would optimize the wrong pipeline.

## Cross-milestone principles

- **No semantic recognition:** no SAM, neural model, class label or subject classifier returns to the active path.
- **No external SLIC dependency:** the proprietary deterministic implementation remains authoritative.
- **Immutable accepted plans:** preview and final export always reuse the same geometry.
- **Continuous visual transitions:** discrete roles and passes may guide budgets, but may not create visible region seams.
- **Budget before allocation:** memory and work admission precede expensive buffers, candidates and plan collections.
- **Deterministic composition:** stable identifiers, stable tie-breaking and derived seeds are mandatory.
- **Persist intent, recompute derivations:** projects save settings and user edits, not redundant full label maps.
- **Measure before optimizing:** tiling, parallelism and local high-resolution SLIC remain conditional on profiling.

## Delivery sequence

### 1. M15.3 — Staged Flow rendering

Create the painterly order that the current single-pass Flow mode lacks. The first pass establishes broad colour motion; the second describes regional structure; the third reinforces only important contours; the fourth adds local detail.

Primary risk: four passes could simply overpaint one another and increase visual noise. Mitigation: explicit budgets, pass-specific eligibility and manual comparison against the M15.2 single-pass compatibility path.

### 2. M15.4 — Primitive coarse-to-fine

Apply the hierarchy to primitive optimization after the Flow pass contracts are stable. Broad candidates must not erase strong boundaries; small candidates must be reserved for regions that justify their cost.

Primary risk: multiplying optimization stages can explode candidate work. Mitigation: conserve one total budget, reserve it before allocation and cap each stage independently.

### 3. M15.5 — Unified artistic hierarchy

Remove duplicated artistic decisions from the individual engines. One immutable allocation result maps regional evidence and manual roles into Broad, Supporting, Protected, Focal and Critical treatment.

Primary risk: discrete classes can create seams. Mitigation: use classes for planning/budget ownership but publish continuous influence weights for rendering.

### 4. M16 — Advanced regional editing

Only after the hierarchy is stable should the user edit it directly. Begin with selection/navigation and compact role overrides; then add topology changes, freehand masks/barriers and command history.

Primary risk: editing derived labels can produce brittle project files. Mitigation: persist source-relative operations, identifiers/fingerprints and manual overrides, then deterministically reconstruct derived data.

### 5. M17 — High-resolution optimization and release

Refine proxy boundaries at source resolution, add precise invalidation/caching, profile memory and CPU, introduce deterministic parallelism only where measured, then finish recovery and packaging.

Primary risk: attempting full-resolution segmentation of 100 million pixels. Mitigation: global proxy segmentation plus narrow-band refinement remains the default architecture.

## Decision gates

After each milestone, continue only when:

- build is warning-free;
- the complete suite passes;
- deterministic identity is proven;
- work/memory estimates remain conservative;
- manual output comparison shows no new seams or mechanical outlines;
- documentation and schema compatibility are updated.

M15.3 is the only approved immediate coding target.
