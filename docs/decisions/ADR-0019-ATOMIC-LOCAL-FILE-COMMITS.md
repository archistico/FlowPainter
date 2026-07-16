# ADR-0019 — Atomic local-file commits

## Status

Accepted, implemented and validated by M13.4.3 with 790 passing tests.

## Context

FlowPainter previously opened the selected destination directly for project, preset, preview, raster, SVG and recent-item writes. Opening a destination for writing can truncate an existing valid file before serialization or encoding completes. Cancellation, process failure, codec failure or an I/O exception could therefore leave a partial or empty file at the user's chosen path.

SLIC and later editing milestones will increase the value and cost of project state. Persistence must be hardened before those changes are introduced.

## Decision

All local destination writes use one Application-level `AtomicFileWriter`.

The writer creates a unique temporary sibling, writes through the existing stream serializer/encoder, flushes and closes it, then performs a same-directory replace/move to the final path. The visible destination is not touched before commit.

On failure or cancellation, the temporary file is deleted best-effort and the existing destination is preserved. The primary exception is never replaced by a cleanup exception.

The desktop shell supplies `IStorageFile.Path.LocalPath`. Non-local providers are not silently downgraded to direct streaming; they require a future provider-specific transactional contract.

## Alternatives considered

### Continue writing directly to the selected destination

Rejected. It can truncate valid user data before the new content is complete.

### Serialize entirely to memory, then write the destination

Rejected as the general solution. High-resolution PNG/JPEG and SVG output can be large, would duplicate memory pressure and would still require safe destination replacement.

### Keep a permanent `.bak` file for every write

Deferred. Backup retention and recovery policy belong to autosave/recovery work. Atomic replacement protects the previous visible destination during the current operation without introducing backup lifecycle decisions.

### Use a temporary directory on the system drive

Rejected. Cross-filesystem moves may degrade into copy/delete operations and lose atomic replacement semantics. The temporary file must be a sibling of the destination.

### Fall back to direct streaming for virtual storage providers

Rejected. Silent downgrade would make persistence guarantees dependent on picker implementation. A provider must expose an explicit transactional capability before it is supported.

## Consequences

Positive:

- failed saves and exports do not corrupt an existing destination;
- cancellation has a clear transactional boundary;
- project dirty state is cleared only after commit;
- stream serializers and encoders remain filesystem-independent;
- one policy protects all current local output formats;
- M14 can add regional state without inheriting unsafe writes.

Costs:

- a temporary sibling requires enough free disk space for a second copy during commit;
- the final move can still fail because of permissions, locks or filesystem errors;
- exact crash durability after the filesystem reports success depends on the operating system and filesystem;
- non-local storage providers require separate transactional support.

These costs are accepted in favour of preserving the previous valid file until a complete replacement is ready.
