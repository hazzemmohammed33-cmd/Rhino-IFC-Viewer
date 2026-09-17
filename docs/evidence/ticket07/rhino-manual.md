# Ticket 07: Observed Rhino Host Checks — 2026-09-17

## Build identity

- Loaded assembly: `src/RhinoIfcViewer/bin/Release/net8.0-windows/RhinoIfcViewer.rhp`.
- Actual host: Rhino 8 evaluation, Windows x64 running .NET 8.
- Test helper: `.scratch/ticket07/verify-host-ticket07.py`.
- Results JSON: [host-checks.json](host-checks.json) — **7/7 PASS**.
- Operates only on private scratch fixtures. The original sample fixtures were not modified.

## Verified A6, A10 & A11 Assertions (7/7 PASS)

1. **Baseline Load:** Embedded IFC loads initially with 4 exported objects, 1 skipped curve, valid scene available, and valid IFC file on disk (`Objects: 4 exported · 1 skipped`).
2. **Missing WASM Asset Fault:** Renaming `web-ifc.wasm` triggers truthful viewer error (`Failed to fetch`); scene is marked unavailable; valid IFC path remains preserved in status bar.
3. **Asset Recovery:** Restoring `web-ifc.wasm` and refreshing recovers clean loaded state with 3D model rendered and scene available (`Loaded with exclusions`).
4. **Renderer Crash Fault:** Real DevTools `Page.crash` triggers `ProcessFailed` (`RenderProcessExited`); `_browserNeedsReset` is set to true; scene availability cleared; valid IFC path retained.
5. **Crash Recovery (`ResetBrowser`):** Refresh recreates a fresh `WebView2` control, disposes the crashed control (`oldDisposed=True`), and renders the IFC cleanly (`newControl=True, loaded=True`).
6. **Protocol Rejection:** Protocol validation strictly rejects unknown message types (`False`), mismatched revisions (`False`), empty error messages (`False`); accepts valid loaded (`True`); rejects late messages after completion (`False`).
7. **Document Switch Invalidation:** Switching documents permanently clears displayed scene and metadata; no late message can resurrect old preview.

## Automated Test Suites

- C# Unit Tests: **PASS 29/29** (Includes `ViewerOperationTests`: protocol validation, document serial mismatch, cancellation, failure retention, and invalidation tests).
- Node Contract Tests: **PASS 18/18** (URL routing, parameters, terminal error reporting).
- Release Build: **PASS 0 warnings, 0 errors** (`dotnet build -c Release -warnaserror`).
