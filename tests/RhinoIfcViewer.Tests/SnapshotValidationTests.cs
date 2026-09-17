using System;
using System.IO;
using System.Threading;
using Xunit;

namespace RhinoIfcViewer.Tests;

public sealed class SnapshotValidationTests
{
    [Fact]
    public void NonFinite_vertex_throws_with_object_name_and_id()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "bad_nan.ifc");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new IfcExporter().Write(FixtureSnapshots.MalformedNaN(), finalPath, CancellationToken.None));

            Assert.Contains("Bad_NaN", ex.Message);
            Assert.Contains("non-finite", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(finalPath));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void OutOfRange_index_throws_with_object_name_and_id()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "bad_index.ifc");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new IfcExporter().Write(FixtureSnapshots.MalformedIndex(), finalPath, CancellationToken.None));

            Assert.Contains("Bad_Index", ex.Message);
            Assert.Contains("out-of-range", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(finalPath));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Duplicate_corner_indices_throw_with_object_name_and_id()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "bad_duplicate.ifc");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new IfcExporter().Write(FixtureSnapshots.MalformedDuplicateCorners(), finalPath, CancellationToken.None));

            Assert.Contains("Bad_Duplicate", ex.Message);
            Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(finalPath));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void ZeroArea_triangle_throws_with_object_name_and_id()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "bad_collinear.ifc");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new IfcExporter().Write(FixtureSnapshots.MalformedZeroArea(), finalPath, CancellationToken.None));

            Assert.Contains("Bad_Collinear", ex.Message);
            Assert.Contains("zero", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(finalPath));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Empty_triangles_throw_with_object_name_and_id()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "bad_empty.ifc");
        try
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                new IfcExporter().Write(FixtureSnapshots.MalformedEmptyTriangles(), finalPath, CancellationToken.None));

            Assert.Contains("Bad_Empty", ex.Message);
            Assert.Contains("0 triangles", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.False(File.Exists(finalPath));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}
