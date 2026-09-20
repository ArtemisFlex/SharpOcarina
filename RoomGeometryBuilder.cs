using System;
using System.Collections.Generic;
using OpenTK;

namespace SharpOcarina
{
    /// <summary>
    /// Authoring primitives shared by the visual room model and the collision model.
    /// Coordinates use SharpOcarina's existing room coordinate system.
    /// </summary>
    public enum RoomPrimitiveType
    {
        Plane,
        Triangle,
        Cube,
        Ramp
    }

    public sealed class RoomPrimitiveSpec
    {
        public RoomPrimitiveType Type = RoomPrimitiveType.Cube;
        public string Name = "Primitive";
        public string MaterialName = "Default";
        public string TexturePath = null;
        public Vector3d Min = new Vector3d(-50, 0, -50);
        public Vector3d Max = new Vector3d(50, 100, 50);
        public Vector3d A = new Vector3d(-50, 0, -50);
        public Vector3d B = new Vector3d(50, 0, -50);
        public Vector3d C = new Vector3d(0, 0, 50);
        public int CollisionPolyType = 0;
        public bool AddToCollision = true;
    }

    public static class RoomGeometryBuilder
    {
        private sealed class Face
        {
            public Vector3d[] Points;
            public Vector3d Normal;
            public string MaterialName;
            public Face(Vector3d[] points, Vector3d normal, string materialName)
            {
                Points = points;
                Normal = normal;
                MaterialName = materialName;
            }
        }

        public static void Append(ObjFile visual, ObjFile collision, RoomPrimitiveSpec spec)
        {
            if (visual == null) throw new ArgumentNullException("visual");
            if (spec == null) throw new ArgumentNullException("spec");

            string groupName = string.IsNullOrEmpty(spec.Name) ? "Primitive" : spec.Name;
            string materialName = string.IsNullOrEmpty(spec.MaterialName) ? "Default" : spec.MaterialName;
            EnsureMaterial(visual, materialName, spec.TexturePath);

            List<Face> faces = BuildFaces(spec, materialName);
            AppendFaces(visual, groupName, faces, false, spec.CollisionPolyType);

            if (collision != null && spec.AddToCollision)
                AppendFaces(collision, groupName + "_Collision", faces, true, spec.CollisionPolyType);
        }

        public static void AppendAll(ObjFile visual, ObjFile collision, IEnumerable<RoomPrimitiveSpec> primitives)
        {
            if (primitives == null) return;
            foreach (RoomPrimitiveSpec primitive in primitives)
                Append(visual, collision, primitive);
        }

        private static List<Face> BuildFaces(RoomPrimitiveSpec spec, string materialName)
        {
            switch (spec.Type)
            {
                case RoomPrimitiveType.Plane:
                    return new List<Face>
                    {
                        MakeFace(new Vector3d[]
                        {
                            new Vector3d(spec.Min.X, spec.Min.Y, spec.Min.Z),
                            new Vector3d(spec.Max.X, spec.Min.Y, spec.Min.Z),
                            new Vector3d(spec.Max.X, spec.Min.Y, spec.Max.Z),
                            new Vector3d(spec.Min.X, spec.Min.Y, spec.Max.Z)
                        }, materialName)
                    };
                case RoomPrimitiveType.Triangle:
                    return new List<Face> { MakeFace(new Vector3d[] { spec.A, spec.B, spec.C }, materialName) };
                case RoomPrimitiveType.Ramp:
                    return BuildRamp(spec, materialName);
                default:
                    return BuildCube(spec, materialName);
            }
        }

        private static List<Face> BuildCube(RoomPrimitiveSpec spec, string materialName)
        {
            Vector3d a = new Vector3d(spec.Min.X, spec.Min.Y, spec.Min.Z);
            Vector3d b = new Vector3d(spec.Max.X, spec.Min.Y, spec.Min.Z);
            Vector3d c = new Vector3d(spec.Max.X, spec.Min.Y, spec.Max.Z);
            Vector3d d = new Vector3d(spec.Min.X, spec.Min.Y, spec.Max.Z);
            Vector3d e = new Vector3d(spec.Min.X, spec.Max.Y, spec.Min.Z);
            Vector3d f = new Vector3d(spec.Max.X, spec.Max.Y, spec.Min.Z);
            Vector3d g = new Vector3d(spec.Max.X, spec.Max.Y, spec.Max.Z);
            Vector3d h = new Vector3d(spec.Min.X, spec.Max.Y, spec.Max.Z);

            return new List<Face>
            {
                MakeFace(new Vector3d[] { a, d, c, b }, materialName),
                MakeFace(new Vector3d[] { e, f, g, h }, materialName),
                MakeFace(new Vector3d[] { a, b, f, e }, materialName),
                MakeFace(new Vector3d[] { b, c, g, f }, materialName),
                MakeFace(new Vector3d[] { c, d, h, g }, materialName),
                MakeFace(new Vector3d[] { d, a, e, h }, materialName)
            };
        }

        // The ramp rises from Min.Z to Max.Z. Min/Max still define its footprint.
        private static List<Face> BuildRamp(RoomPrimitiveSpec spec, string materialName)
        {
            Vector3d a = new Vector3d(spec.Min.X, spec.Min.Y, spec.Min.Z);
            Vector3d b = new Vector3d(spec.Max.X, spec.Min.Y, spec.Min.Z);
            Vector3d c = new Vector3d(spec.Max.X, spec.Min.Y, spec.Max.Z);
            Vector3d d = new Vector3d(spec.Min.X, spec.Min.Y, spec.Max.Z);
            Vector3d e = new Vector3d(spec.Min.X, spec.Max.Y, spec.Max.Z);
            Vector3d f = new Vector3d(spec.Max.X, spec.Max.Y, spec.Max.Z);

            return new List<Face>
            {
                MakeFace(new Vector3d[] { a, d, c, b }, materialName),
                MakeFace(new Vector3d[] { a, b, f, e }, materialName),
                MakeFace(new Vector3d[] { b, c, f }, materialName),
                MakeFace(new Vector3d[] { c, d, e, f }, materialName),
                MakeFace(new Vector3d[] { d, a, e }, materialName)
            };
        }

        private static Face MakeFace(Vector3d[] points, string materialName)
        {
            Vector3d normal = Vector3d.Cross(points[1] - points[0], points[2] - points[0]);
            if (normal.LengthSquared > 0.000001)
                normal.Normalize();
            return new Face(points, normal, materialName);
        }

        private static void AppendFaces(ObjFile model, string groupName, List<Face> faces, bool collision, int polyType)
        {
            ObjFile.Group group = new ObjFile.Group { Name = groupName, PolyType = polyType };
            group.BackfaceCulling = !collision;

            foreach (Face face in faces)
            {
                int[] vertexIndices = new int[face.Points.Length];
                int[] texCoordIndices = new int[face.Points.Length];
                int[] normalIndices = new int[face.Points.Length];

                for (int i = 0; i < face.Points.Length; i++)
                {
                    Vector3d point = face.Points[i];
                    vertexIndices[i] = model.Vertices.Count;
                    model.Vertices.Add(new ObjFile.Vertex(point.X, point.Y, point.Z));

                    texCoordIndices[i] = model.TextureCoordinates.Count;
                    model.TextureCoordinates.Add(new ObjFile.TextureCoord(i == 1 || i == 2 ? 1 : 0, i >= 2 ? 1 : 0));

                    normalIndices[i] = model.Normals.Count;
                    model.Normals.Add(new ObjFile.Normal(face.Normal.X, face.Normal.Y, face.Normal.Z));
                }

                for (int i = 1; i < face.Points.Length - 1; i++)
                {
                    int[] triangleVertices = { vertexIndices[0], vertexIndices[i], vertexIndices[i + 1] };
                    int[] triangleUvs = { texCoordIndices[0], texCoordIndices[i], texCoordIndices[i + 1] };
                    int[] triangleNormals = { normalIndices[0], normalIndices[i], normalIndices[i + 1] };
                    ObjFile.Triangle triangle = new ObjFile.Triangle(face.MaterialName, triangleVertices, triangleUvs, triangleNormals);
                    group.Triangles.Add(triangle);
                }
            }

            model.Groups.Add(group);
        }

        private static void EnsureMaterial(ObjFile model, string name, string texturePath)
        {
            foreach (ObjFile.Material material in model.Materials)
            {
                if (string.Equals(material.Name, name, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(texturePath)) material.map_Kd = texturePath;
                    return;
                }
            }

            ObjFile.Material created = new ObjFile.Material { Name = name, map_Kd = texturePath };
            model.Materials.Add(created);
        }
    }
}
