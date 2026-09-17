# Rhino host verification — Ticket 04 (2026-09-16)

Tested Ticket 04 end-to-end IFC4 generation and embedded WebView2 preview in Rhino 8 using `IfcExporter` and `ExportAndPreviewDiagnosticAsync`.

- **PASS — Snapshot Reading in Live Host:**
  - Read `assessment.3dm` via `RhinoMeshReader.Read(doc)`: 4 supported objects extracted, 1 unsupported curve excluded.

- **PASS — IFC4 Generation via xBIM (A4/A7):**
  - Generated IFC4 STEP export with in-memory `IfcStore`, SI metre units, neutral spatial hierarchy (Project -> Site -> Building -> Storey).
  - Triangulated face sets (`IfcTriangulatedFaceSet`) created with 1-based face indices.
  - Temporary file reopened, validated against IFC4 schema and closed before atomic publication.

- **PASS — Embedded WebView2 3D Rendering (A3/A4):**
  - Navigated local virtual host `https://rhino-ifc.local/` with published IFC revision.
  - That Open Company engine initialized, parsed the IFC, and rendered the 3D geometry inside the embedded WebView2 window.
  - Received matching `loaded` web message from browser without errors.

- **PASS — Execution Performance & Completion:**
  - End-to-end export and preview completed in 4751 ms (well under 60-second viewer timeout).
  - Recorded evidence in `docs/evidence/ticket04/exporter-checks.json` (Overall: PASS, 4/4 assertions passed).
