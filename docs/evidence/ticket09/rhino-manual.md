# Ticket 09: Observed Rhino Host Checks — 2026-09-17

## Build identity

- Loaded assembly: `src/RhinoIfcViewer/bin/Release/net8.0-windows/RhinoIfcViewer.rhp`.
- Actual host: Rhino 8 evaluation, Windows x64 running .NET 8.
- Test helper: `.scratch/ticket09/verify-host-ticket09.py`.
- Results JSON: [host-checks.json](host-checks.json) — **11/11 PASS**.
- Operates only on private scratch fixtures. The original sample fixtures were not modified.

## Verified A15 Assertions (11/11 PASS)

1. **Busy State Presentation:** Action button disabled, text is `"Working..."`, cursor is `WaitCursor`, and status text is styled in busy blue (`#2563EB`).
2. **Window Properties & Hierarchy:** Modeless Rhino-owned window with approved native caption `"Rhino IFC Viewer"`, default client size 960×700, minimum window size 680×500, `AutoScaleMode = AutoScaleMode.Dpi`, `ShowInTaskbar = false`.
3. **Typography & Color Palette:** Segoe UI typography, background `#F5F6F8`, foreground `#202124`, primary button `#2563EB`, and white header/footer panels.
4. **Action Button Specification:** Single primary action button `"Export & Refresh"`, `UseMnemonic = false` (never clips with accelerator), minimum size 160×38 logical pixels, corner radius 8px.
5. **Tab Order & Accessibility:** Sequential focus traversal Button (`TabIndex = 0`) → WebView2 Browser (`TabIndex = 1`) → PathBox (`TabIndex = 2`). Core accessible names present (`"Export and refresh"`, `"IFC model preview"`, `"Exported IFC file path"`, `"Snapshot source"`, `"Preview status"`).
6. **Source Document Label & ToolTip:** Shows active document name, `AutoEllipsis = true`, full document path/name displayed in hover tooltip without truncation.
7. **Loaded Real IFC Scene State:** Real That Open Company IFC scene rendered in WebView2 browser control, message panel hidden, status reflects truthful exclusion warning (`#8A5A00`), export summary visible (`"Objects: 4 exported · 1 skipped"`), valid existing `.ifc` file path populated.
8. **Selectable Long Path:** Exported path control is a single-line `TextBox` with `ReadOnly = true` and `BorderStyle = FixedSingle`, allowing keyboard and mouse selection and copying of long filesystem paths.
9. **Stale Snapshot State:** On export failure (triggered by degenerate geometry), surviving WebView2 browser scene is retained, orange stale indicator `"Previous snapshot (stale)"` (`#8A5A00`) is visible, status displays error in red (`#B42318`), and previous valid path is preserved.
10. **Document Switch Idle State:** Switching to another document cleanly clears the scene, hides the browser, displays sibling message panel with `"Document changed"`, clears the path box and export summary, and restores button to enabled `"Export & Refresh"`.
11. **Empty State & Minimum Size Enforcement:** On exporting an empty document, sibling message panel is visible with `"Empty document"` and `"No supported 3D geometry found in document."`, summary displays truthful `"Objects: 0 exported · 0 skipped"`, and path is empty. Enforcing minimum size down to 680×500 maintains non-clipped action button (`182×47` actual size) and auto-wrapping status label constraints.

## Automated Test Suites

- C# Unit Tests: **PASS 29/29** (`dotnet test tests/RhinoIfcViewer.Tests`).
- Node Contract Tests: **PASS 18/18** (`npm test` in `viewer/`).
- Release Build: **PASS 0 warnings, 0 errors** (`dotnet build -c Release -warnaserror`).
