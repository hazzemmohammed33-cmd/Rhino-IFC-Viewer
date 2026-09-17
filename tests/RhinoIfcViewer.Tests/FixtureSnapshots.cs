using System;
using System.Collections.Generic;
using RhinoIfcViewer;

namespace RhinoIfcViewer.Tests;

internal static class FixtureSnapshots
{
    public static ModelSnapshot Cube()
    {
        Vertex3[] vertices =
        [
            new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0),
            new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)
        ];
        Triangle[] triangles =
        [
            new(0, 2, 1), new(0, 3, 2), new(4, 5, 6), new(4, 6, 7),
            new(0, 1, 5), new(0, 5, 4), new(1, 2, 6), new(1, 6, 5),
            new(2, 3, 7), new(2, 7, 6), new(3, 0, 4), new(3, 4, 7)
        ];
        var item = new MeshObject(Guid.NewGuid(), "Cube", vertices, triangles, true);
        return new ModelSnapshot(1, "Cube fixture", new[] { item }, Array.Empty<string>());
    }

    public static ModelSnapshot TwoCubes()
    {
        Vertex3[] v1 =
        [
            new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0),
            new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)
        ];
        Vertex3[] v2 =
        [
            new(5, 0, 0), new(6, 0, 0), new(6, 1, 0), new(5, 1, 0),
            new(5, 0, 1), new(6, 0, 1), new(6, 1, 1), new(5, 1, 1)
        ];
        Triangle[] triangles =
        [
            new(0, 2, 1), new(0, 3, 2), new(4, 5, 6), new(4, 6, 7),
            new(0, 1, 5), new(0, 5, 4), new(1, 2, 6), new(1, 6, 5),
            new(2, 3, 7), new(2, 7, 6), new(3, 0, 4), new(3, 4, 7)
        ];
        var item1 = new MeshObject(Guid.NewGuid(), "Cube_Origin", v1, triangles, true);
        var item2 = new MeshObject(Guid.NewGuid(), "Cube_Offset5m", v2, triangles, true);
        return new ModelSnapshot(2, "Two cubes fixture", new[] { item1, item2 }, Array.Empty<string>());
    }

    public static ModelSnapshot OpenSurface()
    {
        Vertex3[] vertices =
        [
            new(0, 3, 1), new(1, 3, 1), new(1, 4, 1), new(0, 4, 1)
        ];
        Triangle[] triangles =
        [
            new(0, 1, 2), new(0, 2, 3)
        ];
        var item = new MeshObject(Guid.NewGuid(), "Open_Surface", vertices, triangles, false);
        return new ModelSnapshot(3, "Open surface fixture", new[] { item }, Array.Empty<string>());
    }

    public static ModelSnapshot MalformedNaN()
    {
        Vertex3[] vertices = [new(double.NaN, 0, 0), new(1, 0, 0), new(0, 1, 0)];
        Triangle[] triangles = [new(0, 1, 2)];
        var item = new MeshObject(Guid.NewGuid(), "Bad_NaN", vertices, triangles, false);
        return new ModelSnapshot(4, "Bad NaN", new[] { item }, Array.Empty<string>());
    }

    public static ModelSnapshot MalformedIndex()
    {
        Vertex3[] vertices = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)];
        Triangle[] triangles = [new(0, 1, 99)]; // Out of range index
        var item = new MeshObject(Guid.NewGuid(), "Bad_Index", vertices, triangles, false);
        return new ModelSnapshot(5, "Bad Index", new[] { item }, Array.Empty<string>());
    }

    public static ModelSnapshot MalformedDuplicateCorners()
    {
        Vertex3[] vertices = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)];
        Triangle[] triangles = [new(0, 1, 1)]; // Duplicate corners
        var item = new MeshObject(Guid.NewGuid(), "Bad_Duplicate", vertices, triangles, false);
        return new ModelSnapshot(6, "Bad Duplicate", new[] { item }, Array.Empty<string>());
    }

    public static ModelSnapshot MalformedZeroArea()
    {
        Vertex3[] vertices = [new(0, 0, 0), new(1, 0, 0), new(2, 0, 0)]; // Collinear
        Triangle[] triangles = [new(0, 1, 2)];
        var item = new MeshObject(Guid.NewGuid(), "Bad_Collinear", vertices, triangles, false);
        return new ModelSnapshot(7, "Bad Collinear", new[] { item }, Array.Empty<string>());
    }

    public static ModelSnapshot MalformedEmptyTriangles()
    {
        Vertex3[] vertices = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)];
        Triangle[] triangles = Array.Empty<Triangle>();
        var item = new MeshObject(Guid.NewGuid(), "Bad_Empty", vertices, triangles, false);
        return new ModelSnapshot(8, "Bad Empty", new[] { item }, Array.Empty<string>());
    }
}
