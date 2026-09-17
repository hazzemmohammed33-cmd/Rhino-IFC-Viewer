using System;
using System.Collections.Generic;

namespace RhinoIfcViewer;

public readonly record struct Vertex3(double X, double Y, double Z);
public readonly record struct Triangle(int A, int B, int C);

public sealed record MeshObject(
    Guid SourceId,
    string Name,
    IReadOnlyList<Vertex3> VerticesMetres,
    IReadOnlyList<Triangle> Triangles,
    bool IsClosed);

public sealed record ModelSnapshot(
    uint DocumentSerial,
    string DocumentName,
    IReadOnlyList<MeshObject> Objects,
    IReadOnlyList<string> Exclusions);

public sealed record ExportResult(string FilePath, int ExportedCount);
