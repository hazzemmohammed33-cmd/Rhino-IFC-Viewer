# Rhino host verification — Ticket 03 (2026-09-16)

Tested Ticket 03 geometry capture from live Rhino active document using `RhinoMeshReader`.

- **PASS — Assessment Fixture Generation:**
  - Created `samples/assessment.3dm` (metres model unit system) with 5 authored objects: `Box_1m`, `Extrusion_2m`, `Open_Surface`, `Offset_Mesh`, `Unsupported_Curve`.
  - Created `samples/assessment-mm.3dm` (millimetres model unit system) with identical physical dimensions scaled to mm (1000 units box, 5000 offset mesh).

- **PASS — UI Thread Capture Performance (A2/A7 Target):**
  - Synchronous reading and meshing pause on metres fixture: **1 ms** (acceptance target <= 2000 ms).
  - Synchronous reading and meshing pause on millimetres fixture: **1 ms**.

- **PASS — Supported Geometry Extraction & Triangulation:**
  - Extracted 4 supported objects:
    - `Box_1m`: Closed Brep box with `IsClosed == true`, bounds [0, 1] metres.
    - `Extrusion_2m`: Extrusion converted to Brep, height spans Z [0, 2] metres.
    - `Open_Surface`: Planar corner surface with verified `IsClosed == false`.
    - `Offset_Mesh`: Mesh quad converted to 2 triangles, spanning X [5, 6] metres at Z = 0.5 metres.

- **PASS — In-Boundary Unsupported Element Diagnostics (A9):**
  - Accurately identified 1 unsupported element: `Unsupported Curve 'Unsupported_Curve' (25260318-94a7-4b04-90af-d416bc02e00b)`.
  - Excluded from exported geometry without causing export failure or crashing.

- **PASS — Non-Destructive Operation (Zero Document Mutation):**
  - Source document dirty flag `doc.Modified` remained unmodified.
  - Active document object count remained identical.
  - User object selection state remained identical.
  - Zero undo records created by reader execution.

- **PASS — Unit Conversion to Metres (A7):**
  - Millimetre fixture objects converted accurately to SI metres: Box scaled to [0, 1] metres (not 1000), Offset mesh scaled to [5, 6] metres (not 5000).

- Recorded evidence in `docs/evidence/ticket03/reader-checks.json` (Overall: PASS, 19/19 assertions passed).
