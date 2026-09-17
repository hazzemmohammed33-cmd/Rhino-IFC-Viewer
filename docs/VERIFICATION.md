# Verification — approved tickets and current evidence

This report follows the approved ten-ticket breakdown. Internal planning and working files are intentionally excluded from the public repository. T0–T7 are implementation stages, not ticket numbers. Historical evidence folders retain their original names; their contents do not establish completion of similarly numbered approved tickets.

## Approved ticket coverage

| Ticket | Approved behavior | Evidence and remaining gates |
|---|---|---|
| 01 | Owned modeless singleton window | Historical host ownership/edit/close/reopen evidence in `evidence/ticket01/rhino-manual.md`; current host regression recorded separately |
| 02 | Real IFC rendering with bundled assets | Historical embedded rendering in `evidence/ticket02/rhino-manual.md`; network-disabled run and complete web license inventory not established by local URLs alone |
| 03 | Brep export/refresh plus permanent source invalidation | Historical writer/refresh evidence in folders ticket04/05; operation unit tests cover invalidation and rejected acknowledgements; actual source-switch/closure during writing/loading requires host evidence |
| 04 | Full fixture geometry and truthful exclusions | Historical reader evidence in ticket03 and writer evidence in ticket04; lights are included in enumeration; current full-fixture host checks recorded separately |
| 05 | Empty/unsupported-only snapshots | Implemented clear path and rejection of unexpected empty acknowledgement; host checks must prove scene/path clearing and retry |
| 06 | Export failure and surviving-scene recovery | PASS: 10/10 host assertions in [Ticket 06 evidence](evidence/ticket06/rhino-manual.md); invalid geometry aborts export with name/ID/reason, surviving scene preserved as stale, recovery on repair, no-scene error path verified |
| 07 | Viewer failure, retry and timeout | PASS: 7/7 host assertions in [Ticket 07 evidence](evidence/ticket07/rhino-manual.md); WASM fault/recovery, renderer crash/recovery, protocol rejection, document switch invalidation, 60-second timeout implemented; actual expiry test not present in these seven assertions |
| 08 | Comprehensive lifecycle | PASS: 11/11 host assertions in [Ticket 08 evidence](evidence/ticket08/rhino-manual.md); no auto-refresh on edit, add/delete/move cycles, document switch/switch-back invalidation, form close/reopen capture, repeat cycles, busy-switch rejection |
| 09 | Native visual refinement | PASS: 11/11 host assertions in [Ticket 09 evidence](evidence/ticket09/rhino-manual.md); caption, 680x500 minimum size enforcement, DPI scaling, accessible names, tab order, source tooltip, selectable path, busy/loaded/stale/empty/switch states |
| 10 | Reproducible packaged delivery | PASS: Automated locked packaging via `scripts/package.ps1` to `dist/RhinoIfcViewer` with full manifest, native loader, local web bundle, notices, sample fixtures, generated IFCs, and the reported A1–A15 summary in [Ticket 10 evidence](evidence/ticket10/release-verification.md) |

## Current review corrections

- All supported-object conversion exceptions include object name/ID and original reason, retaining the inner exception.
- A failed copy into the viewer session preserves an existing eligible scene and its matching metadata as stale. Without a scene, valid export-file metadata remains explicitly exported rather than displayed.
- A failed WebView2 process invalidates scene availability and makes the next refresh recreate the browser and initialize a fresh session. Detached browser callbacks cannot affect the replacement.
- Historical stage numbering no longer claims that Ticket 05 completed all lifecycle, recovery and visual work.

## Automated verification — 2026-09-17 (latest code-only review)

| Check | Result | Scope |
|---|---|---|
| Object conversion diagnostic regression | PASS | Observed RED before fix (missing object ID), then GREEN |
| C# suite | PASS: 29/29 | IFC writer/validation, session paths, operation ownership and conversion diagnostics |
| Release build, warnings as errors | PASS (isolated output) | `artifacts/review-current`; normal output was locked by the running Rhino process |
| Node suite | PASS: 18/18 | Request contracts and terminal/runtime-error reporting |
| TypeScript and bundled viewer build | PASS | Vite reports a bundle-size advisory; not an embedded rendering test |
| Previous Rhino host checks | Historical PASS: 13/13 | See [observed host evidence](evidence/review-fixes/rhino-manual.md); staging failure/retry, renderer crash/retry, snapshots and empty states |
| Supplied Ticket 06 Rhino host checks | Recorded PASS: 10/10 | See [Ticket 06 evidence](evidence/ticket06/rhino-manual.md); degenerate area failure with name/ID/reason, stale retention, repair recovery, staging failure/recovery, unknown units no-scene path, idle document-switch clearing |
| Ticket 07 host checks | PASS: 7/7 | See [Ticket 07 evidence](evidence/ticket07/rhino-manual.md); WASM fault/recovery, renderer crash/recovery, protocol rejection; timeout runtime verification NOT RUN in that helper |
| Ticket 08 host checks | PASS: 11/11 | See [Ticket 08 evidence](evidence/ticket08/rhino-manual.md); edit without auto-refresh, add/move/delete, document switch/switch-back, form close/reopen |
| Ticket 09 host checks | PASS: 11/11 | See [Ticket 09 evidence](evidence/ticket09/rhino-manual.md); native caption, 680x500 minimum size, DPI scaling, accessible names, tab order, long path, stale/empty states |
| Ticket 10 packaging & sweep | PASS | See [Ticket 10 evidence](evidence/ticket10/release-verification.md); build/manifest evidence; full A1-A15 acceptance not established |

## Historical evidence

- [Owned window](evidence/ticket01/rhino-manual.md): previous native host checks.
- [Embedded viewer](evidence/ticket02/rhino-manual.md): previous IFC smoke rendering.
- [Geometry capture](evidence/ticket03/rhino-manual.md): previous 19 assertions, four objects/one curve and metre/millimetre fixtures; recorded capture was 1 ms.
- [IFC writer](evidence/ticket04/rhino-manual.md): previous 20 unit tests and end-to-end rendering.
- [Refresh](evidence/ticket05/rhino-manual.md): previous initial/manual refresh. `refresh-checks.json` is syntactically valid with nine recorded assertions.
- [Ticket 06](evidence/ticket06/rhino-manual.md): 10/10 host assertions; export failure diagnostics, surviving scene stale retention, and repair recovery.
- [Ticket 07](evidence/ticket07/rhino-manual.md): 7/7 host assertions; viewer faults, retry, renderer recovery, and 60-second timeout.
- [Ticket 08](evidence/ticket08/rhino-manual.md): 11/11 host assertions; lifecycle matrix, document switches, form close/reopen, repeat cycles.
- [Ticket 09](evidence/ticket09/rhino-manual.md): 11/11 host assertions; native visual design, layout constraints, accessibility, and real-state feedback.
- [Ticket 10](evidence/ticket10/release-verification.md): complete reproducible distribution, manifest SHA-256 verification, and A1–A15 sweep.

## Final acceptance status

Implementation and recorded host assertions are available for all ten workstreams, but a complete A1–A15 acceptance claim is not supported by the supplied evidence. Local asset URLs do not prove network-disabled execution; protocol rejection does not prove a real 60-second timeout; successful repeated refreshes alone do not measure resource leakage. A complete DPI matrix and fresh-host packaged installation also require separate observed evidence. This public-review preparation uses automated checks only, with no Computer Use or Rhino run.

## GitHub review preparation — 2026-09-17

Current automated checks: C# 29/29 PASS, Node 18/18 PASS, TypeScript PASS, isolated Release build PASS with zero warnings/errors. PowerShell package-script parsing PASS. Packaging now invokes npm test and -warnaserror as documented. No new Rhino or Computer Use verification was performed. The complete packaging script was not rerun in this preparation; existing packaged output is historical. Internal planning/work folders are intentionally omitted from Git; their references in historical evidence identify local test provenance, not files supplied to the reviewer.
