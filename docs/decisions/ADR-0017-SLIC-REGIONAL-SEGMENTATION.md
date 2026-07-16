# ADR-0017 — Deterministic SLIC regional segmentation

## Status

Accepted for the M13.4–M17 roadmap. Implementation begins after M13.4 stabilization.

## Context

FlowPainter needs a complete image partition that can guide painterly scale, boundary protection, stroke direction, primitive allocation and manual editing. The application does not require category labels such as person, animal, tree or building. The validated M8–M13.3 heuristic semantic path can identify generic subject-like evidence, but it is fragile as an object recognizer and does not provide a complete, non-overlapping regional partition.

The segmentation solution must:

- run locally and deterministically;
- require no training dataset, model file, Python environment or GPU;
- cover every analysis pixel exactly once;
- produce connected regions with controllable scale;
- expose adjacency and boundary evidence;
- support hierarchy from fine regions to broad painterly masses;
- remain practical for sources up to 10,000 × 10,000 through proxy-first processing;
- integrate without coupling Domain or rendering to a specific imaging library.

## Decision

Use **SLIC (Simple Linear Iterative Clustering)** as the only planned automatic segmentation algorithm.

The first implementation operates on an aspect-ratio-preserving analysis proxy and clusters pixels in CIELAB + image-coordinate space. SLIC produces fine superpixels that are normalized for connectivity, described, connected through a Region Adjacency Graph and merged deterministically into hierarchy levels.

The planned pipeline is:

```text
Analysis proxy
    ↓
SLIC fine superpixels
    ↓
Connectivity repair and compact relabelling
    ↓
Regional colour / luminance / texture / edge descriptors
    ↓
Region Adjacency Graph and shared-boundary strength
    ↓
Deterministic hierarchical merge
    ↓
Manual region-role overrides
    ↓
Boundary, importance, suppression and artistic-detail policies
```

## Architectural placement

- Domain contains immutable region identities, compact label-map abstractions, descriptors, hierarchy and role values that do not reference SkiaSharp or Avalonia.
- Application contains `IRegionSegmentationAnalyzer`, SLIC, connectivity repair, descriptors, graph construction, merge policies, memory estimation and orchestration.
- Imaging.Skia remains responsible for decode and proxy generation only.
- Rendering.Skia never interprets SLIC labels; it rasterizes approved immutable Flow, Primitive or Hybrid plans.
- App exposes controls, overlays and editing commands and owns native visual resources.

## Core invariants

A successful `RegionSegmentationResult` must guarantee:

- dimensions equal the requested analysis proxy;
- every pixel has one valid region identifier;
- no region identifier is empty;
- identifiers are compact and deterministic;
- every final fine-level region is connected;
- summed region areas equal image area;
- adjacency is symmetric and contains only shared borders;
- hierarchy parentage is traceable and acyclic;
- source maps and pixel sources are never mutated.

## Storage and memory

- use `UInt16` labels when the result has at most 65,536 regions;
- use `UInt32` only when required;
- reject unsupported memory/work estimates before allocation;
- compute global segmentation on a proxy rather than allocating full-resolution Lab or floating-point fields;
- project labels and boundaries to source coordinates;
- defer high-resolution border refinement and local resegmentation to M17;
- do not persist large derived label maps initially; persist settings and manual intent and rebuild deterministically.

## Migration from M8–M13.3

The current semantic analyzer remains operational until M14.7. Migration rules are:

- schema-1 through schema-11 projects remain readable;
- M13.3 manual corrections are durable user intent and become generalized region-role overrides;
- automatic semantic regions are derived evidence and need not be retained;
- semantic settings may be read for compatibility but stop influencing new plans after migration;
- Flow, Primitive and Hybrid switch together to one shared regional pipeline.

## Alternatives considered

### SAM, MobileSAM and other trained segmentation models

Rejected for the approved roadmap. They add model distribution, inference runtime, hardware variability, overlapping-mask reconciliation and non-deterministic operational complexity without providing necessary value for class-agnostic painterly regions.

### New ONNX or class-aware semantic provider

Rejected. FlowPainter does not need labels, and improving primary-subject recognition would preserve the wrong abstraction rather than provide a complete partition.

### Felzenszwalb as the main algorithm

Rejected for the initial implementation. Its adaptive regions can be useful, but region counts and sizes are less predictable. SLIC gives a controllable fine partition better suited to deterministic descriptors, adjacency and hierarchical merging.

### Raw SLIC labels as the final artistic regions

Rejected. Fine superpixels are building blocks. FlowPainter requires descriptors, boundary strengths and multiple hierarchy levels so broad masses and local details can use different scales.

## Consequences

Positive:

- no ML, model, Python or GPU dependency;
- complete non-overlapping label map;
- deterministic and testable behaviour;
- predictable scale and memory policy;
- natural basis for adjacency, boundaries and regional editing;
- one regional hierarchy shared by all generative engines.

Costs:

- SLIC does not understand semantic objects;
- low-contrast object boundaries may require structural evidence or manual roles;
- connectivity repair, graph construction and merge policies add substantial engineering work;
- proxy-to-source projection and high-resolution refinement require dedicated validation.

These costs are accepted because the project needs painterly structure and editable regions, not automatic labels.
