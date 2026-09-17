using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xunit;

namespace RhinoIfcViewer.Tests;

public sealed class IfcExporterTests
{
    [Fact]
    public void Cube_publishes_a_reopenable_twelve_triangle_proxy()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "cube.ifc");
        try
        {
            var result = new IfcExporter().Write(FixtureSnapshots.Cube(), finalPath, CancellationToken.None);
            Assert.Equal(finalPath, result.FilePath);
            Assert.Equal(1, result.ExportedCount);
            Assert.True(File.Exists(finalPath));

            using var model = IfcStore.Open(finalPath);
            Assert.Equal(XbimSchemaVersion.Ifc4, model.SchemaVersion);

            var proxy = Assert.Single(model.Instances.OfType<IIfcBuildingElementProxy>());
            Assert.Equal("Cube", proxy.Name);

            var faces = Assert.Single(model.Instances.OfType<IIfcTriangulatedFaceSet>());
            Assert.True(faces.Closed);
            Assert.Equal(12, faces.CoordIndex.Count());

            Assert.NotNull(faces.Coordinates);
            var coords = faces.Coordinates;
            Assert.Equal(8, coords.CoordList.Count());

            // Metre units check
            var project = Assert.Single(model.Instances.OfType<IIfcProject>());
            Assert.NotNull(project.UnitsInContext);
            var unitAssign = project.UnitsInContext;
            var lengthUnit = Assert.Single(unitAssign.Units.OfType<IIfcSIUnit>(), u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
            Assert.Equal(IfcSIUnitName.METRE, lengthUnit.Name);

            // Verify coordinate bounds [0, 1]
            foreach (var pt in coords.CoordList)
            {
                var xyz = pt.Select(m => (double)m.Value).ToList();
                Assert.Equal(3, xyz.Count);
                Assert.All(xyz, v => Assert.InRange(v, 0.0, 1.0));
            }
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void TwoCubes_publishes_two_unique_proxies_with_retained_coordinates()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "two_cubes.ifc");
        try
        {
            var result = new IfcExporter().Write(FixtureSnapshots.TwoCubes(), finalPath, CancellationToken.None);
            Assert.Equal(2, result.ExportedCount);
            Assert.True(File.Exists(finalPath));

            using var model = IfcStore.Open(finalPath);
            var proxies = model.Instances.OfType<IIfcBuildingElementProxy>().ToList();
            Assert.Equal(2, proxies.Count);

            // Unique GlobalIds
            Assert.NotEqual(proxies[0].GlobalId, proxies[1].GlobalId);

            var originCube = Assert.Single(proxies, p => p.Name == "Cube_Origin");
            var offsetCube = Assert.Single(proxies, p => p.Name == "Cube_Offset5m");

            // Check offset cube coordinate spans 5..6 in X
            var offsetFaceSet = offsetCube.Representation.Representations
                .SelectMany(r => r.Items).OfType<IIfcTriangulatedFaceSet>().First();
            var xs = offsetFaceSet.Coordinates.CoordList.Select(pt => (double)pt.First().Value).ToList();
            Assert.Equal(5.0, xs.Min());
            Assert.Equal(6.0, xs.Max());
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void OpenSurface_publishes_open_face_set()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "open_surface.ifc");
        try
        {
            var result = new IfcExporter().Write(FixtureSnapshots.OpenSurface(), finalPath, CancellationToken.None);
            Assert.Equal(1, result.ExportedCount);

            using var model = IfcStore.Open(finalPath);
            var faces = Assert.Single(model.Instances.OfType<IIfcTriangulatedFaceSet>());
            Assert.False(faces.Closed);
            Assert.Equal(2, faces.CoordIndex.Count());
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Hierarchy_contains_project_site_building_storey()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "hierarchy.ifc");
        try
        {
            new IfcExporter().Write(FixtureSnapshots.Cube(), finalPath, CancellationToken.None);

            using var model = IfcStore.Open(finalPath);
            var project = Assert.Single(model.Instances.OfType<IIfcProject>());
            var site = Assert.Single(model.Instances.OfType<IIfcSite>());
            var building = Assert.Single(model.Instances.OfType<IIfcBuilding>());
            var storey = Assert.Single(model.Instances.OfType<IIfcBuildingStorey>());

            // Project contains Site
            var relSite = Assert.Single(model.Instances.OfType<IIfcRelAggregates>(), r => r.RelatingObject == project);
            Assert.Contains(site, relSite.RelatedObjects);

            // Site contains Building
            var relBldg = Assert.Single(model.Instances.OfType<IIfcRelAggregates>(), r => r.RelatingObject == site);
            Assert.Contains(building, relBldg.RelatedObjects);

            // Building contains Storey
            var relStorey = Assert.Single(model.Instances.OfType<IIfcRelAggregates>(), r => r.RelatingObject == building);
            Assert.Contains(storey, relStorey.RelatedObjects);

            // Storey contains Proxy
            var relContained = Assert.Single(model.Instances.OfType<IIfcRelContainedInSpatialStructure>(), r => r.RelatingStructure == storey);
            var proxy = Assert.Single(model.Instances.OfType<IIfcBuildingElementProxy>());
            Assert.Contains(proxy, relContained.RelatedElements);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void FaceIndices_are_valid_and_one_based()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "face_indices.ifc");
        try
        {
            new IfcExporter().Write(FixtureSnapshots.Cube(), finalPath, CancellationToken.None);

            using var model = IfcStore.Open(finalPath);
            var faceSet = Assert.Single(model.Instances.OfType<IIfcTriangulatedFaceSet>());
            int pointCount = faceSet.Coordinates.CoordList.Count();

            foreach (var face in faceSet.CoordIndex)
            {
                var indices = face.Select(i => (long)i.Value).ToList();
                Assert.Equal(3, indices.Count);
                Assert.All(indices, idx => Assert.InRange(idx, 1, pointCount));
            }
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Destination_is_existing_regular_file_throws()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var existingFile = Path.Combine(folder, "existing_file.txt");
        File.WriteAllText(existingFile, "content");
        var badPath = Path.Combine(existingFile, "output.ifc");

        try
        {
            Assert.Throws<IOException>(() =>
                new IfcExporter().Write(FixtureSnapshots.Cube(), badPath, CancellationToken.None));
            Assert.False(File.Exists(badPath));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void Cancellation_before_or_during_publish_cleans_up()
    {
        var folder = Path.Combine(Path.GetTempPath(), "RhinoIfcViewerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var finalPath = Path.Combine(folder, "cancelled.ifc");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            Assert.Throws<OperationCanceledException>(() =>
                new IfcExporter().Write(FixtureSnapshots.Cube(), finalPath, cts.Token));

            Assert.False(File.Exists(finalPath));
            // Assert no temporary files left in the directory
            Assert.Empty(Directory.GetFiles(folder));
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }
    }
}
