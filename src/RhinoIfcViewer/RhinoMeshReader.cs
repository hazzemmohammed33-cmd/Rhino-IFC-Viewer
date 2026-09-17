using System;
using System.Collections.Generic;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoIfcViewer;

public sealed class RhinoMeshReader
{
    public ModelSnapshot Read(RhinoDoc document)
    {
        ArgumentNullException.ThrowIfNull(document);

        uint serial = document.RuntimeSerialNumber;
        string documentName = string.IsNullOrWhiteSpace(document.Name) ? "Untitled" : document.Name;

        var settings = new ObjectEnumeratorSettings
        {
            NormalObjects = true,
            HiddenObjects = true,
            LockedObjects = true,
            ActiveObjects = true,
            ReferenceObjects = false,
            IncludeLights = true,
            IncludeGrips = false
        };

        var supportedCandidates = new List<RhinoObject>();
        var exclusions = new List<string>();

        foreach (var obj in document.Objects.GetObjectList(settings))
        {
            if (obj == null || obj.IsDeleted || obj.IsInstanceDefinitionGeometry || obj.IsReference)
                continue;

            GeometryBase? geom = obj.Geometry;
            if (geom == null)
                continue;

            if (geom is Mesh or Brep or Extrusion or Surface)
            {
                supportedCandidates.Add(obj);
            }
            else
            {
                string displayName = !string.IsNullOrWhiteSpace(obj.Attributes.Name)
                    ? obj.Attributes.Name
                    : obj.Id.ToString();
                exclusions.Add($"Unsupported {geom.ObjectType} '{displayName}' ({obj.Id})");
            }
        }

        if (supportedCandidates.Count == 0)
        {
            return new ModelSnapshot(serial, documentName, Array.Empty<MeshObject>(), exclusions);
        }

        UnitSystem unitSystem = document.ModelUnitSystem;
        if (unitSystem is UnitSystem.None or UnitSystem.Unset or UnitSystem.CustomUnits || (int)unitSystem <= 0)
        {
            throw new InvalidOperationException(
                $"Document '{documentName}' has unknown or unsupported model units ({unitSystem}). Supported geometry cannot be scaled to metres.");
        }

        double scaleToMetres = RhinoMath.UnitScale(unitSystem, UnitSystem.Meters);
        if (!double.IsFinite(scaleToMetres) || scaleToMetres <= 0.0)
        {
            throw new InvalidOperationException(
                $"Document '{documentName}' has invalid unit scale factor {scaleToMetres} to metres.");
        }

        var meshObjects = new List<MeshObject>(supportedCandidates.Count);

        foreach (var obj in supportedCandidates)
        {
            GeometryBase geom = obj.Geometry!;
            string displayName = !string.IsNullOrWhiteSpace(obj.Attributes.Name)
                ? obj.Attributes.Name
                : obj.Id.ToString();

            Mesh? combinedMesh = null;
            ObjectConversion.Run(obj.Id, displayName, () =>
            {
                try
                {
                    if (geom is Mesh sourceMesh)
                    {
                        combinedMesh = sourceMesh.DuplicateMesh();
                    }
                    else if (geom is Brep brep)
                    {
                        var pieces = Mesh.CreateFromBrep(brep, MeshingParameters.Default);
                        if (pieces == null || pieces.Length == 0)
                        {
                            throw new InvalidOperationException(
                                $"Supported Brep '{displayName}' ({obj.Id}) produced no mesh.");
                        }
                        combinedMesh = new Mesh();
                        foreach (var piece in pieces)
                        {
                            combinedMesh.Append(piece);
                            piece.Dispose();
                        }
                    }
                    else if (geom is Extrusion extrusion)
                    {
                        using var brepFromExtrusion = extrusion.ToBrep();
                        if (brepFromExtrusion == null)
                        {
                            throw new InvalidOperationException(
                                $"Supported Extrusion '{displayName}' ({obj.Id}) could not be converted to Brep.");
                        }
                        var pieces = Mesh.CreateFromBrep(brepFromExtrusion, MeshingParameters.Default);
                        if (pieces == null || pieces.Length == 0)
                        {
                            throw new InvalidOperationException(
                                $"Supported Extrusion '{displayName}' ({obj.Id}) produced no mesh.");
                        }
                        combinedMesh = new Mesh();
                        foreach (var piece in pieces)
                        {
                            combinedMesh.Append(piece);
                            piece.Dispose();
                        }
                    }
                    else if (geom is Surface surface)
                    {
                        using var brepFromSurface = surface.ToBrep();
                        if (brepFromSurface == null)
                        {
                            throw new InvalidOperationException(
                                $"Supported Surface '{displayName}' ({obj.Id}) could not be converted to Brep.");
                        }
                        var pieces = Mesh.CreateFromBrep(brepFromSurface, MeshingParameters.Default);
                        if (pieces == null || pieces.Length == 0)
                        {
                            throw new InvalidOperationException(
                                $"Supported Surface '{displayName}' ({obj.Id}) produced no mesh.");
                        }
                        combinedMesh = new Mesh();
                        foreach (var piece in pieces)
                        {
                            combinedMesh.Append(piece);
                            piece.Dispose();
                        }
                    }

                    if (combinedMesh == null)
                    {
                        throw new InvalidOperationException(
                            $"Supported object '{displayName}' ({obj.Id}) could not produce a mesh.");
                    }

                    combinedMesh.Faces.ConvertQuadsToTriangles();
                    combinedMesh.Compact();

                    bool isClosed = combinedMesh.IsClosed;
                    int vertexCount = combinedMesh.Vertices.Count;
                    if (vertexCount == 0)
                    {
                        throw new InvalidOperationException(
                            $"Supported object '{displayName}' ({obj.Id}) has 0 mesh vertices.");
                    }

                    var verticesMetres = new Vertex3[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Point3d pt = combinedMesh.Vertices.Point3dAt(i);
                        double x = pt.X * scaleToMetres;
                        double y = pt.Y * scaleToMetres;
                        double z = pt.Z * scaleToMetres;

                        if (!double.IsFinite(x) || !double.IsFinite(y) || !double.IsFinite(z))
                        {
                            throw new InvalidOperationException(
                                $"Supported object '{displayName}' ({obj.Id}) has non-finite coordinate at vertex {i}: ({x}, {y}, {z}).");
                        }

                        verticesMetres[i] = new Vertex3(x, y, z);
                    }

                    int faceCount = combinedMesh.Faces.Count;
                    if (faceCount == 0)
                    {
                        throw new InvalidOperationException(
                            $"Supported object '{displayName}' ({obj.Id}) has 0 mesh faces.");
                    }

                    var triangles = new List<Triangle>(faceCount);
                    for (int f = 0; f < faceCount; f++)
                    {
                        MeshFace face = combinedMesh.Faces[f];
                        if (!face.IsTriangle)
                        {
                            throw new InvalidOperationException(
                                $"Supported object '{displayName}' ({obj.Id}) face {f} is not a triangle after quad conversion.");
                        }

                        int a = face.A;
                        int b = face.B;
                        int c = face.C;

                        if (a < 0 || a >= vertexCount || b < 0 || b >= vertexCount || c < 0 || c >= vertexCount)
                        {
                            throw new InvalidOperationException(
                                $"Supported object '{displayName}' ({obj.Id}) triangle {f} has out-of-range index ({a}, {b}, {c}) for vertex count {vertexCount}.");
                        }

                        if (a == b || b == c || a == c)
                        {
                            throw new InvalidOperationException(
                                $"Supported object '{displayName}' ({obj.Id}) triangle {f} has duplicate vertex index ({a}, {b}, {c}).");
                        }

                        Vertex3 va = verticesMetres[a];
                        Vertex3 vb = verticesMetres[b];
                        Vertex3 vc = verticesMetres[c];

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
                                $"Supported object '{displayName}' ({obj.Id}) triangle {f} has zero or degenerate area.");
                        }

                        triangles.Add(new Triangle(a, b, c));
                    }

                    if (triangles.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Supported object '{displayName}' ({obj.Id}) produced no triangles.");
                    }

                    meshObjects.Add(new MeshObject(obj.Id, displayName, verticesMetres, triangles, isClosed));
                }
                finally
                {
                    combinedMesh?.Dispose();
                }
            });
        }

        return new ModelSnapshot(serial, documentName, meshObjects, exclusions);
    }
}
