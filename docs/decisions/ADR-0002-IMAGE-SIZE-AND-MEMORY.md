# ADR-0002: Image size and memory boundary

- Status: Accepted
- Date: 2026-07-13

## Decision

Decoded source and output images are limited to 10,000 × 10,000 pixels. The application is designed for a 64-bit process. Analysis and previews use reduced proxies; full-size float detail maps are not allowed.

Tiled rendering is deferred until measurement demonstrates that it is necessary.

## Consequences

- A single maximum-size RGBA buffer is approximately 381 MiB.
- Memory estimates are shown before expensive final rendering.
- Full-resolution output remains feasible without introducing premature out-of-core complexity.
