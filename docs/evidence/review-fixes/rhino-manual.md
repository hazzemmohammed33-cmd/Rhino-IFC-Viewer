# Review corrections: observed Rhino checks — 2026-09-17

## Build identity

- Loaded assembly: `src/RhinoIfcViewer/bin/Release/net8.0-windows/RhinoIfcViewer.rhp`.
- Module version ID: `74626297-ff51-4688-8d21-e197f67ee05e`.
- SHA-256: `1b833024a079da621b3897a177840e2e9026d72523e66dbece73ad5f758f4acf`; independently matched the build output after the run.
- Actual host: Rhino 8 evaluation, 88 days remaining. Started and controlled with the Computer Use skill; the helper ran via Rhino's `_-RunPythonScript` command.
- Test helper: `.scratch/review-fixes/verify-host.py`. It uses a separate copied assessment file, stops rather than discarding an existing dirty document, and does not ship with the plug-in.

## PASS — 13 actual assertions

See [host-checks.json](host-checks.json) for each assertion and actual diagnostic text.

1. Actual IFC rendered inside embedded WebView2, with four exported objects and one unsupported skip.
2. Repeated command retained the same form/snapshot.
3. Direct full small-fixture capture took **9 ms**, below the 2000 ms acceptance target. This is not an export/load duration or a larger-model guarantee.
4. Geometry reader returned four objects/one exclusion.
5. Deliberately obstructing the session `models` directory made File.Copy fail; the existing scene remained visible with matching old path and stale label.
6. The user-visible error identified the staging failure.
7. Restoring the directory and refreshing loaded a new snapshot and cleared stale state.
8. A real DevTools `Page.crash` produced `RenderProcessExited`; scene eligibility cleared and the valid IFC path remained.
9. Refresh created a different WebView2 control, disposed the old one and rendered the IFC successfully.
10. Adding a box did not automatically refresh.
11. Manual refresh produced five exported objects and a new path.
12. Removing the scratch geometry and refreshing cleared the scene and path, reporting 0 exported/0 skipped.
13. A unitless scratch document containing a curve and point light reported 0 exported/2 skipped, with no old scene/path.

Computer Use screenshots also showed the real rendered model and final empty/unsupported-only state. The private scratch model was saved; the original fixtures were not edited.

## Recorder corrections, not product failures

Initial evidence recording failed on IronPython's handling of the middle-dot character in the summary string. The incomplete first output is retained as `host-checks-incomplete-first-run.txt`; it is not PASS evidence. The helper now normalizes non-ASCII text for JSON, completes serialization before writing and records terminal failures without repeatedly restarting its timer. The final JSON is from a fresh completed run.

## Automated checks

- C# tests: **PASS, 25/25**.
- New object-conversion diagnostic test: observed **RED** (object ID absent), then **GREEN** after contextual exception handling.
- Node tests: **PASS, 18/18**.
- TypeScript check and Vite production build: **PASS**. Vite emits its large-chunk advisory; this is not a compile failure.
- Native Release build with `--no-restore -warnaserror`: **PASS**, zero warnings/errors.
- Whitespace verification for changed C# implementation files: **PASS**.

## NOT RUN / limits

These checks do not complete every ticket or the final acceptance matrix. Browser-process exit (distinct from the tested renderer-process exit), full source switch-away/back and close during writing/loading, actual 60-second timeout, network-disabled operation, comprehensive DPI/accessibility checks and a fresh packaged installation were not run in this correction pass. Supported native conversion diagnostics are covered at the exception boundary by unit tests; no native mesher exception was forced in this host run.

No commit, push or submission was performed.
