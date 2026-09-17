# Rhino IFC Viewer - decision record

Workspace preparation: approved decisions are retained under master version 1.5. Current scope is local project references and a proposed tracer-bullet breakdown only; implementation has not begun.

## ADR - Approved architecture and baseline

**Status:** Explicitly approved by Hazem before the interview.

1. Ashraf explicitly confirmed Windows, That Open and geometry-focused export. Hazem approved the remaining proposed architecture choices.
2. Capture-time snapshots; later edits require another refresh; no automatic geometry-change detection.
3. IFC writing and viewer loading remain asynchronous; the capture target is specified below.
4. Preserve a previous successful scene only while it exists; otherwise show an error and retain valid export metadata without claiming it is displayed.
5. Skipped counts cover unsupported objects inside the enumeration boundary only. Deleted objects, definition members and linked/reference-document geometry are outside it.
6. Use one box, extrusion, open surface and mesh plus one unsupported curve. Offset one supported object; use equivalent millimetre/metre fixtures.
7. Prove embedded IFC rendering before visual refinement. Audit resolved dependencies after each relevant restore; finalize notices during packaging.
8. Superseded by the latest scheduling decision below: Hazem manages assessment time and budget. The agent must not track effort or stop on a time budget.

## Interview question 1 - Invalid supported geometry

**Question:** Fail the new export completely, or export valid objects when a supported object cannot be meshed or fails validation?

**Answer:** Fail the current export. Report the offending object by name/ID and failure reason. Do not count it as an unsupported skip. Preserve the previous successful preview if available, mark it stale and allow retry. Otherwise apply the approved error and valid-file-metadata fallback.

**Authority:** Hazem explicitly accepted this recommendation.

**Consequence / acceptance:** No partial new export is reported as successful. A10 checks object diagnostics, no successful new publication, correct skip classification, retained-scene recovery and retry after correction.

## Interview question 2 - Maximum capture pause

**Question:** Is a two-second capture pause acceptable for the agreed small assessment model?

**Answer:** Two seconds is the maximum acceptable pause, measured during testing as an acceptance target. It is not a guarantee for larger models. Writing and viewer loading remain asynchronous.

**Authority:** Hazem explicitly accepted this recommendation.

**Consequence / acceptance:** Measure elapsed synchronous capture/meshing time until UI control returns; record duration and machine in A2/A3. Exceeding the target is an acceptance failure to investigate and fix under the approved scope without an agent-managed time limit. No measured result is claimed yet.

## Interview question 3 - Document switching while idle

**Question:** When the viewer displays A and the user activates B while idle, clear A immediately or keep its labelled snapshot until refresh?

**Answer:** Clear A's preview and displayed counts/path; identify B and prompt Export & Refresh. Do not export automatically. Keep already-written IFC files on disk. If no active document remains, show a neutral no-document state. The same identity/lifetime rules invalidate pending work during export/load.

**Authority:** Adopted under Hazem's instruction to answer remaining questions using the recommendations, and retained in the subsequently requested specification.

**Consequence / acceptance:** A11 covers switching both idle and busy; old messages cannot restore the wrong document's scene. This does not introduce detection of geometry edits.

## ADR - Owner-managed assessment scheduling

**Status:** Accepted from Hazem's latest direct instruction; supersedes earlier effort-ceiling and accounting decisions.

**Decision:** Hazem manages the deadline and time budget. No agent effort tracking, preparation accounting, eight-hour stop, task deadline or reserve applies. After implementation authorization, complete integration, testing and required fixes; report verified results and limitations.

**Boundary:** The current to-tickets request says do not implement yet. Product requirements to measure the two-second capture pause and enforce the 60-second viewer timeout remain. This scheduling instruction does not establish what the employer historically counted as preparation.

## Review summary

The three interview questions are answered and the approved architecture is unchanged. Master version 1.5 retains them and the latest scheduling override. Technical runtime checks remain unperformed. No further interview was conducted during synthesis. Ticket planning uses local Markdown in the current workspace. No external tracker setup or publication is required. The proposed breakdown awaits review before individual ticket files are created.

## Skill availability

The requested grill-with-docs skill was read. Its grilling and domain-modeling helper skills were not found in installed skill locations. The interview followed Hazem's direct instructions with this decision record and the glossary; unavailable helper invocations are not claimed.
