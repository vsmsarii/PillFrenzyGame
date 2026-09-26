using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace PillFrenzy.Gameplay
{
    public struct PathMeshSettings
    {
        public Mesh SegmentMesh;
        public Mesh StartMesh;
        public Mesh EndMesh;
        public float Width;
        public float Thickness;
        public float SurfaceOffset;
        public float SamplesPerUnit;
        public float UvTileLength;
    }

    public sealed class PathMeshBuilder
    {
        private readonly List<Vector3> m_Vertices = new();
        private readonly List<Vector3> m_Normals = new();
        private readonly List<Vector2> m_Uvs = new();
        private readonly List<int> m_Triangles = new();

        private readonly List<Vector3> m_SourceVertices = new();
        private readonly List<Vector3> m_SourceNormals = new();
        private readonly List<Vector2> m_SourceUvs = new();
        private readonly List<int> m_SourceTriangles = new();

        public void Build(Spline spline, PathMeshSettings settings, Mesh target)
        {
            m_Vertices.Clear();
            m_Normals.Clear();
            m_Uvs.Clear();
            m_Triangles.Clear();

            float length = spline.GetLength();
            if (spline.Count >= 2 && length > 0f)
            {
                if (settings.SegmentMesh != null)
                    AddDeformedSegments(spline, length, settings);
                else
                    AddProceduralBody(spline, length, settings);

                AddCap(spline, settings, settings.StartMesh, 0f, false);
                AddCap(spline, settings, settings.EndMesh, 1f, true);
            }

            target.Clear();
            target.indexFormat = m_Vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            target.SetVertices(m_Vertices);
            target.SetNormals(m_Normals);
            target.SetUVs(0, m_Uvs);
            target.SetTriangles(m_Triangles, 0);
            target.RecalculateBounds();
            target.RecalculateTangents();
        }

        private void AddProceduralBody(Spline spline, float length, PathMeshSettings settings)
        {
            int rings = Mathf.Max(2, Mathf.CeilToInt(length * settings.SamplesPerUnit) + 1);
            float halfWidth = settings.Width * 0.5f;
            float top = settings.SurfaceOffset;
            float bottom = top - settings.Thickness;

            Vector2[] profile =
            {
                new(-halfWidth, top), new(halfWidth, top),
                new(halfWidth, top), new(halfWidth, bottom),
                new(halfWidth, bottom), new(-halfWidth, bottom),
                new(-halfWidth, bottom), new(-halfWidth, top)
            };
            Vector2[] faceNormals = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

            for (int face = 0; face < 4; face++)
            {
                int faceStart = m_Vertices.Count;
                Vector2 a = profile[face * 2];
                Vector2 b = profile[face * 2 + 1];

                for (int ring = 0; ring < rings; ring++)
                {
                    float t = ring / (float)(rings - 1);
                    Frame frame = Evaluate(spline, t);
                    float v = t * length / settings.UvTileLength;

                    m_Vertices.Add(frame.Transform(a));
                    m_Vertices.Add(frame.Transform(b));
                    Vector3 normal = frame.TransformDirection(faceNormals[face]);
                    m_Normals.Add(normal);
                    m_Normals.Add(normal);
                    m_Uvs.Add(new Vector2(0f, v));
                    m_Uvs.Add(new Vector2(1f, v));
                }

                for (int ring = 0; ring < rings - 1; ring++)
                {
                    int i0 = faceStart + ring * 2;
                    AddQuad(i0, i0 + 2, i0 + 3, i0 + 1);
                }
            }
        }

        private void AddDeformedSegments(Spline spline, float length, PathMeshSettings settings)
        {
            Mesh source = settings.SegmentMesh;
            Bounds bounds = source.bounds;
            float sourceLength = Mathf.Max(bounds.size.z, 0.0001f);
            int tiles = Mathf.Max(1, Mathf.RoundToInt(length / sourceLength));
            float tileLength = length / tiles;

            ReadSource(source);
            for (int tile = 0; tile < tiles; tile++)
            {
                int offset = m_Vertices.Count;
                for (int i = 0; i < m_SourceVertices.Count; i++)
                {
                    Vector3 vertex = m_SourceVertices[i];
                    float along = (vertex.z - bounds.min.z) / sourceLength;
                    Frame frame = Evaluate(spline, (tile + along) * tileLength / length);
                    m_Vertices.Add(frame.Transform(new Vector2(vertex.x, vertex.y + settings.SurfaceOffset)));
                    m_Normals.Add(frame.TransformDirection(m_SourceNormals[i]));
                    m_Uvs.Add(i < m_SourceUvs.Count ? m_SourceUvs[i] : Vector2.zero);
                }

                for (int i = 0; i < m_SourceTriangles.Count; i++)
                    m_Triangles.Add(offset + m_SourceTriangles[i]);
            }
        }

        private void AddCap(Spline spline, PathMeshSettings settings, Mesh capMesh, float t, bool atEnd)
        {
            Frame frame = Evaluate(spline, t);
            if (!atEnd)
                frame = frame.Reversed();

            if (capMesh == null)
            {
                AddProceduralCap(frame, settings);
                return;
            }

            ReadSource(capMesh);
            int offset = m_Vertices.Count;
            for (int i = 0; i < m_SourceVertices.Count; i++)
            {
                Vector3 vertex = m_SourceVertices[i];
                m_Vertices.Add(frame.Transform(new Vector3(vertex.x, vertex.y + settings.SurfaceOffset, vertex.z)));
                m_Normals.Add(frame.TransformDirection(m_SourceNormals[i]));
                m_Uvs.Add(i < m_SourceUvs.Count ? m_SourceUvs[i] : Vector2.zero);
            }

            for (int i = 0; i < m_SourceTriangles.Count; i++)
                m_Triangles.Add(offset + m_SourceTriangles[i]);
        }

        private void AddProceduralCap(Frame frame, PathMeshSettings settings)
        {
            float halfWidth = settings.Width * 0.5f;
            float top = settings.SurfaceOffset;
            float bottom = top - settings.Thickness;
            Vector3 normal = frame.Forward;
            int start = m_Vertices.Count;

            m_Vertices.Add(frame.Transform(new Vector2(halfWidth, top)));
            m_Vertices.Add(frame.Transform(new Vector2(-halfWidth, top)));
            m_Vertices.Add(frame.Transform(new Vector2(-halfWidth, bottom)));
            m_Vertices.Add(frame.Transform(new Vector2(halfWidth, bottom)));
            for (int i = 0; i < 4; i++)
                m_Normals.Add(normal);

            m_Uvs.Add(new Vector2(1f, 1f));
            m_Uvs.Add(new Vector2(0f, 1f));
            m_Uvs.Add(new Vector2(0f, 0f));
            m_Uvs.Add(new Vector2(1f, 0f));
            AddQuad(start, start + 1, start + 2, start + 3);
        }

        private void ReadSource(Mesh source)
        {
            source.GetVertices(m_SourceVertices);
            source.GetNormals(m_SourceNormals);
            source.GetUVs(0, m_SourceUvs);
            m_SourceTriangles.Clear();
            for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
                m_SourceTriangles.AddRange(source.GetTriangles(subMesh));

            while (m_SourceNormals.Count < m_SourceVertices.Count)
                m_SourceNormals.Add(Vector3.up);
        }

        private void AddQuad(int a, int b, int c, int d)
        {
            m_Triangles.Add(a);
            m_Triangles.Add(b);
            m_Triangles.Add(c);
            m_Triangles.Add(a);
            m_Triangles.Add(c);
            m_Triangles.Add(d);
        }

        private static Frame Evaluate(Spline spline, float t)
        {
            spline.Evaluate(t, out float3 position, out float3 tangent, out float3 up);
            Vector3 forward = math.lengthsq(tangent) > 0f ? math.normalize(tangent) : new float3(0f, 0f, 1f);
            Vector3 right = Vector3.Cross(up, forward).normalized;
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.Cross(Vector3.up, forward).normalized;

            return new Frame(position, forward, Vector3.Cross(forward, right), right);
        }

        private readonly struct Frame
        {
            public readonly Vector3 Position;
            public readonly Vector3 Forward;
            public readonly Vector3 Up;
            public readonly Vector3 Right;

            public Frame(Vector3 position, Vector3 forward, Vector3 up, Vector3 right)
            {
                Position = position;
                Forward = forward;
                Up = up;
                Right = right;
            }

            public Frame Reversed()
            {
                return new Frame(Position, -Forward, Up, -Right);
            }

            public Vector3 Transform(Vector2 point)
            {
                return Position + Right * point.x + Up * point.y;
            }

            public Vector3 Transform(Vector3 point)
            {
                return Position + Right * point.x + Up * point.y + Forward * point.z;
            }

            public Vector3 TransformDirection(Vector3 direction)
            {
                return Right * direction.x + Up * direction.y + Forward * direction.z;
            }
        }
    }
}
