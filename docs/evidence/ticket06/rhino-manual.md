# Ticket 06: Observed Rhino Host Checks — 2026-09-17

## Build identity

- Loaded assembly: `src/RhinoIfcViewer/bin/Release/net8.0-windows/RhinoIfcViewer.rhp`.
- Module Version ID: `74626297-ff51-4688-8d21-e197f67ee05e`.
- SHA-256: `1b833024a079da621b3897a177840e2e9026d72523e66dbece73ad5f758f4acf`.
- Actual host: Rhino 8 evaluation, Windows x64 running .NET 8.
- Test helper: `.scratch/ticket06/verify-host-ticket06.py`.
- Results JSON: [host-checks.json](host-checks.json) — **10/10 PASS**.
- Operates only on private scratch fixtures. The original sample fixtures were not modified.

## Verified A10 Assertions (10/10 PASS)

1. **Baseline Load:** Embedded IFC loads initially with 4 exported objects, 1 skipped curve, valid scene available, and valid IFC file on disk (`Objects: 4 exported · 1 skipped`).
2. **Invalid Supported Object Injection:** Injected supported Mesh `Bad_DegenerateMesh` (`ea54680e-ad6b-4019-a240-f35b05481259`) into active document.
3. **Entire-Export Abort & Actionable Diagnostics:** Export failed completely without partial output; status explicitly identifies object name, GUID, and reason:
   `Export failed: Supported object 'Bad_DegenerateMesh' (ea54680e-ad6b-4019-a240-f35b05481259) conversion failed: Supported object 'Bad_DegenerateMesh' (ea54680e-ad6b-4019-a240-f35b05481259) triangle 0 has zero or degenerate area.`
   Never treated as an unsupported skip.
4. **Surviving Scene Preserved as Stale:** Previous valid scene remains visible in the embedded browser; `Stale` warning badge is displayed; matching previous IFC path and count metadata are preserved (`sceneAvailable=True, stale=True`).
5. **Recovery After Repair:** Deleting the invalid object and refreshing restores clean loaded state, dismisses the stale badge, and generates a new IFC snapshot (`model-44644160d9a549c8b627bb01b8106ba8.ifc`).
6. **Staging / Directory Obstruction Failure:** Deliberately obstructing the session models directory causes staging failure; previous valid scene is preserved with stale warning (`Export failed: Viewer file staging failed...`).
7. **Staging Recovery:** Restoring directory and refreshing recovers clean loaded state (`model-aa698518f6f240ba8c4c1fff0d7caf91.ifc`).
8. **No-Scene Recovery Path (Unknown Units):** Fresh unitless document (`unitless-1789602906.3dm`) with supported geometry aborts export with descriptive unit error (`Export failed: Document 'unitless-1789602906.3dm' has unknown or unsupported model units (None). Supported geometry cannot be scaled to metres.`).
9. **No False Scene Retention:** Without a surviving scene, `sceneAvailable` is false, stale badge is false, and error panel is clearly presented (`sceneAvailable=False, stale=False`).
10. **Idle Document-Switch Clearing:** The helper opens the fixture after the prior refresh has completed and checks that scene availability and stale state are false (`scene cleared on switch`). It does not test a switch during pending work, switch-away-and-back before completion, or delayed callbacks; those remain separate verification gates.

## Automated Test Suites

- C# Unit Tests: **PASS 25/25** (`SnapshotValidationTests`, `ObjectConversionTests`, `IfcExporterTests`, `ViewerSessionTests`, `ViewerOperationTests`).
- Node Contract Tests: **PASS 18/18** (URL routing, parameters, terminal error reporting).
- Release Build: **PASS 0 warnings, 0 errors** (`dotnet build -c Release -warnaserror`).
