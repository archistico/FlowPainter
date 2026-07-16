# Notices and attribution

FlowPainter is a refactor and evolution of the original GPL-3.0 generative-art project included in `legacy/original`.
The original copyright and GPL license are preserved in `LICENSE` and in the legacy source.

FlowPainter uses SkiaSharp, distributed under the MIT License, for image decoding, pixel storage, raster rendering and PNG/JPEG encoding beginning with Milestone 2.

The geometric-primitives engine introduced in Milestone 9 is conceptually inspired by Michael Fogleman's `primitive` project, distributed under the MIT License. The FlowPainter implementation is an original C# design built around this repository's normalized plans, detail maps, deterministic randomness and layer boundaries; no source code from `primitive` is copied into the repository. If source code is adapted in a future change, the applicable upstream copyright and MIT notice must be added here before merging.
