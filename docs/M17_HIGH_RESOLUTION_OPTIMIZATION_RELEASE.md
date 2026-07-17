# M17 — High-resolution optimization, packaging and release

**Status:** PLANNED  
**Dependency:** functional completion of M15 and M16

## M17.1 — Source-resolution boundary refinement

- project proxy labels and hierarchy to source coordinates;
- identify narrow uncertain boundary bands;
- refine boundaries from source gradients without allocating a full floating-point segmentation field;
- optionally run bounded local SLIC only where measured quality requires it;
- preserve normalized plan geometry and role assignments.

## M17.2 — Incremental caches and partial analysis

- define cache keys per segmentation, descriptors, RAG, hierarchy, role composition, boundary field and plan;
- invalidate only stages affected by settings or edits;
- use atomic cache publication and existing generation/stale-result rules;
- make caches optional and reproducible.

## M17.3 — Profiling and deterministic performance

- benchmark representative 4K/8K inputs and opt-in 10K fixtures;
- profile managed allocations, native Skia memory, CPU time and I/O separately;
- parallelize only independent reductions with deterministic merge order;
- introduce overlap-aware tiling only if proxy/refinement measurements fail acceptance targets;
- keep the shared admission budget authoritative.

## M17.4 — Autosave and recovery

- periodic atomic autosave of user intent and workspace state;
- startup recovery selection without overwriting the last explicit save;
- corrupt/incomplete recovery rejection;
- cleanup and retention policy.

## M17.5 — Release engineering

- Windows and Linux publish profiles;
- icon and metadata verification;
- self-contained/framework-dependent packaging decision;
- license/notice review;
- end-user guide, troubleshooting and sample workflow;
- release checklist and reproducible artifact hashes.

## Certification targets

- complete normal CI suite;
- opt-in large-image stress suite up to 10,000 × 10,000;
- deterministic plan hashes across repeated runs on the same runtime/platform;
- memory peak within the documented admission estimate tolerance;
- preview/final plan identity;
- crash/recovery and package-launch smoke tests.
