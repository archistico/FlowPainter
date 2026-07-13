# ADR-0004: SkiaSharp 4 package alignment

- **Status:** accepted
- **Date:** 2026-07-13

## Context

M2 introduces SkiaSharp into a .NET 10 Avalonia 12 application. Avalonia.Skia 12.1.0 declares a lower-bound dependency on SkiaSharp and its Linux native assets. A direct upgrade of the managed `SkiaSharp` package without aligning Linux native assets could allow NuGet to resolve different major versions of the managed and native components.

## Decision

1. Pin `SkiaSharp` to 4.150.0 through central package management.
2. Pin `SkiaSharp.NativeAssets.Linux` to the same 4.150.0 version.
3. Reference the Linux native package directly from the desktop application and native integration-test projects.
4. Keep all SkiaSharp use inside the Imaging and Rendering adapters.
5. Treat an Avalonia startup/render smoke test as part of M2 target validation.

## Consequences

- Managed and Linux native SkiaSharp binaries remain on the same release line.
- Windows and macOS native dependencies continue to flow from the SkiaSharp package.
- Native integration tests can run on the Ubuntu GitHub Actions runner.
- A future SkiaSharp update must update managed and Linux packages together and rerun rendering tests.
