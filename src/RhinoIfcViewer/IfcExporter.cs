using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MeasureResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.UtilityResource;
using Xbim.IO;

namespace RhinoIfcViewer;

public sealed class IfcExporter
{
    public ExportResult Write(
        ModelSnapshot snapshot,
        string finalPath,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(finalPath);

        cancellationToken.ThrowIfCancellationRequested();

        // Defensive validation of detached values
        foreach (var obj in snapshot.Objects)
        {
            int vertexCount = obj.VerticesMetres.Count;
            if (vertexCount == 0)
            {
                throw new InvalidOperationException(
                    $"Object '{obj.Name}' ({obj.SourceId}) has 0 vertices.");
            }

            for (int i = 0; i < vertexCount; i++)
            {
                Vertex3 v = obj.VerticesMetres[i];
                if (!double.IsFinite(v.X) || !double.IsFinite(v.Y) || !double.IsFinite(v.Z))
                {
                    throw new InvalidOperationException(
                        $"Object '{obj.Name}' ({obj.SourceId}) has non-finite coordinate at index {i}: ({v.X}, {v.Y}, {v.Z}).");
                }
            }

            int triangleCount = obj.Triangles.Count;
            if (triangleCount == 0)
            {
                throw new InvalidOperationException(
                    $"Object '{obj.Name}' ({obj.SourceId}) has 0 triangles.");
            }

            for (int f = 0; f < triangleCount; f++)
            {
                Triangle tri = obj.Triangles[f];
                int a = tri.A;
                int b = tri.B;
                int c = tri.C;

                if (a < 0 || a >= vertexCount || b < 0 || b >= vertexCount || c < 0 || c >= vertexCount)
                {
                    throw new InvalidOperationException(
                        $"Object '{obj.Name}' ({obj.SourceId}) triangle {f} has out-of-range index ({a}, {b}, {c}) for vertex count {vertexCount}.");
                }

                if (a == b || b == c || a == c)
                {
                    throw new InvalidOperationException(
                        $"Object '{obj.Name}' ({obj.SourceId}) triangle {f} has duplicate index ({a}, {b}, {c}).");
                }

                Vertex3 va = obj.VerticesMetres[a];
                Vertex3 vb = obj.VerticesMetres[b];
                Vertex3 vc = obj.VerticesMetres[c];

                double abX = vb.X - va.X;
                double abY = vb.Y - va.Y;
                double abZ = vb.Z - va.Z;

                double acX = vc.X - va.X;
                double acY = vc.Y - va.Y;
                double acZ = vc.Z - va.Z;

                double crossX = abY * acZ - abZ * acY;
                double crossY = abZ * acX - abX * acZ;
                double crossZ = abX * acY - abY * acX;

                double crossMagSq = crossX * crossX + crossY * crossY + crossZ * crossZ;
                if (!double.IsFinite(crossMagSq) || crossMagSq <= 1e-24)
                {
                    throw new InvalidOperationException(
                        $"Object '{obj.Name}' ({obj.SourceId}) triangle {f} has zero or degenerate area.");
                }
            }
        }

        string? directory = Path.GetDirectoryName(finalPath);
        if (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(directory))
            {
                throw new IOException($"Target directory path '{directory}' is an existing file.");
            }
            Directory.CreateDirectory(directory);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var credentials = new XbimEditorCredentials
        {
            ApplicationDevelopersName = "OptiDesign",
            ApplicationFullName = "Rhino IFC Viewer",
            ApplicationIdentifier = "RhinoIfcViewer",
            ApplicationVersion = "1.0",
            EditorsFamilyName = "User",
            EditorsGivenName = "Rhino",
            EditorsOrganisationName = "OptiDesign"
        };

        string tempPath = finalPath + $".{Guid.NewGuid():N}.tmp.ifc";

        try
        {
            using (var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel))
            {
                using (var txn = model.BeginTransaction("IFC4 Export"))
                {
                    // Metre units
                    var unitAssignment = model.Instances.New<IfcUnitAssignment>();
                    var metreUnit = model.Instances.New<IfcSIUnit>();
                    metreUnit.UnitType = IfcUnitEnum.LENGTHUNIT;
                    metreUnit.Name = IfcSIUnitName.METRE;
                    unitAssignment.Units.Add(metreUnit);

                    // 3D Geometric Representation Context
                    var context3D = model.Instances.New<IfcGeometricRepresentationContext>();
                    context3D.ContextType = "Model";
                    context3D.CoordinateSpaceDimension = 3;
                    context3D.Precision = 1e-5;

                    var origin = model.Instances.New<IfcCartesianPoint>();
                    origin.SetXYZ(0, 0, 0);
                    var axis = model.Instances.New<IfcDirection>();
                    axis.SetXYZ(0, 0, 1);
                    var refDirection = model.Instances.New<IfcDirection>();
                    refDirection.SetXYZ(1, 0, 0);

                    var worldPlacement = model.Instances.New<IfcAxis2Placement3D>();
                    worldPlacement.Location = origin;
                    worldPlacement.Axis = axis;
                    worldPlacement.RefDirection = refDirection;
                    context3D.WorldCoordinateSystem = worldPlacement;

                    // Project
                    var project = model.Instances.New<IfcProject>();
                    project.GlobalId = Guid.NewGuid();
                    project.Name = string.IsNullOrWhiteSpace(snapshot.DocumentName)
                        ? "Rhino Project"
                        : snapshot.DocumentName;
                    project.UnitsInContext = unitAssignment;
                    project.RepresentationContexts.Add(context3D);

                    // Neutral Spatial Hierarchy: Project -> Site -> Building -> Storey
                    var site = model.Instances.New<IfcSite>();
                    site.GlobalId = Guid.NewGuid();
                    site.Name = "Default Site";
                    var sitePlacement = model.Instances.New<IfcLocalPlacement>();
                    sitePlacement.RelativePlacement = worldPlacement;
                    site.ObjectPlacement = sitePlacement;

                    var relAggSite = model.Instances.New<IfcRelAggregates>();
                    relAggSite.RelatingObject = project;
                    relAggSite.RelatedObjects.Add(site);

                    var building = model.Instances.New<IfcBuilding>();
                    building.GlobalId = Guid.NewGuid();
                    building.Name = "Default Building";
                    var buildingPlacement = model.Instances.New<IfcLocalPlacement>();
                    buildingPlacement.PlacementRelTo = sitePlacement;
                    buildingPlacement.RelativePlacement = worldPlacement;
                    building.ObjectPlacement = buildingPlacement;

                    var relAggBuilding = model.Instances.New<IfcRelAggregates>();
                    relAggBuilding.RelatingObject = site;
                    relAggBuilding.RelatedObjects.Add(building);

                    var storey = model.Instances.New<IfcBuildingStorey>();
                    storey.GlobalId = Guid.NewGuid();
                    storey.Name = "Default Storey";
                    var storeyPlacement = model.Instances.New<IfcLocalPlacement>();
                    storeyPlacement.PlacementRelTo = buildingPlacement;
                    storeyPlacement.RelativePlacement = worldPlacement;
                    storey.ObjectPlacement = storeyPlacement;

                    var relAggStorey = model.Instances.New<IfcRelAggregates>();
                    relAggStorey.RelatingObject = building;
                    relAggStorey.RelatedObjects.Add(storey);

                    var relContained = model.Instances.New<IfcRelContainedInSpatialStructure>();
                    relContained.RelatingStructure = storey;

                    foreach (var obj in snapshot.Objects)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var proxy = model.Instances.New<IfcBuildingElementProxy>();
                        proxy.GlobalId = Guid.NewGuid();
                        proxy.Name = obj.Name;
                        proxy.PredefinedType = IfcBuildingElementProxyTypeEnum.NOTDEFINED;

                        var proxyPlacement = model.Instances.New<IfcLocalPlacement>();
                        proxyPlacement.PlacementRelTo = storeyPlacement;
                        proxyPlacement.RelativePlacement = worldPlacement;
                        proxy.ObjectPlacement = proxyPlacement;

                        var pointList = model.Instances.New<IfcCartesianPointList3D>();
                        foreach (var v in obj.VerticesMetres)
                        {
                            pointList.CoordList.GetAt(pointList.CoordList.Count)
                                .AddRange(new IfcLengthMeasure[] { v.X, v.Y, v.Z });
                        }

                        var faceSet = model.Instances.New<IfcTriangulatedFaceSet>();
                        faceSet.Coordinates = pointList;
                        faceSet.Closed = obj.IsClosed;

                        foreach (var tri in obj.Triangles)
                        {
                            // 1-based indexing in IFC
                            faceSet.CoordIndex.GetAt(faceSet.CoordIndex.Count)
                                .AddRange(new IfcPositiveInteger[] { tri.A + 1, tri.B + 1, tri.C + 1 });
                        }

                        var shapeRep = model.Instances.New<IfcShapeRepresentation>();
                        shapeRep.ContextOfItems = context3D;
                        shapeRep.RepresentationIdentifier = "Body";
                        shapeRep.RepresentationType = "Tessellation";
                        shapeRep.Items.Add(faceSet);

                        var prodShape = model.Instances.New<IfcProductDefinitionShape>();
                        prodShape.Representations.Add(shapeRep);
                        proxy.Representation = prodShape;

                        relContained.RelatedElements.Add(proxy);
                    }

                    txn.Commit();
                }

                cancellationToken.ThrowIfCancellationRequested();
                model.SaveAs(tempPath, StorageType.Ifc);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Reopen validation
            using (var verifyModel = IfcStore.Open(tempPath))
            {
                if (verifyModel.SchemaVersion != XbimSchemaVersion.Ifc4)
                {
                    throw new InvalidOperationException(
                        $"Reopened IFC schema {verifyModel.SchemaVersion} is not IFC4.");
                }

                var proxies = verifyModel.Instances.OfType<IIfcBuildingElementProxy>().ToList();
                if (proxies.Count != snapshot.Objects.Count)
                {
                    throw new InvalidOperationException(
                        $"Reopened proxy count {proxies.Count} does not match snapshot object count {snapshot.Objects.Count}.");
                }

                foreach (var proxy in proxies)
                {
                    var rep = proxy.Representation;
                    if (rep == null || !rep.Representations.Any())
                    {
                        throw new InvalidOperationException(
                            $"Reopened proxy '{proxy.Name}' has empty shape representation.");
                    }

                    var faceSet = rep.Representations.SelectMany(r => r.Items).OfType<IIfcTriangulatedFaceSet>().FirstOrDefault();
                    if (faceSet == null)
                    {
                        throw new InvalidOperationException(
                            $"Reopened proxy '{proxy.Name}' has no IfcTriangulatedFaceSet.");
                    }

                    var coords = faceSet.Coordinates;
                    if (coords == null || !coords.CoordList.Any())
                    {
                        throw new InvalidOperationException(
                            $"Reopened proxy '{proxy.Name}' has empty coordinates.");
                    }

                    int ptCount = coords.CoordList.Count();
                    foreach (var face in faceSet.CoordIndex)
                    {
                        var indices = face.ToList();
                        if (indices.Count != 3)
                        {
                            throw new InvalidOperationException(
                                $"Reopened proxy '{proxy.Name}' face has {indices.Count} indices instead of 3.");
                        }

                        foreach (var idx in indices)
                        {
                            if (idx < 1 || idx > ptCount)
                            {
                                throw new InvalidOperationException(
                                    $"Reopened proxy '{proxy.Name}' face index {idx} out of range 1..{ptCount}.");
                            }
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            File.Move(tempPath, finalPath, overwrite: false);
            return new ExportResult(finalPath, snapshot.Objects.Count);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw;
        }
    }
}
