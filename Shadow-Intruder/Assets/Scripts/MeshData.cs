using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Terrain
{
    public class MeshData
    {
        Vector3[] vertices;
        Vector2[] uvs;
        Vector3[] normals;
        int[] triangles;

        public Vector3[] borderVertices;
        int[] borderTriangles;

        int vertexCount;

        public MeshData(int vertexCount)
        {
            this.vertexCount = vertexCount;
            vertices = new Vector3[vertexCount * vertexCount];
            uvs = new Vector2[vertices.Length];
            normals = new Vector3[vertices.Length];
            triangles = new int[(vertexCount - 1) * (vertexCount - 1) * 6];

            borderVertices = new Vector3[vertexCount * 4 + 4];
            borderTriangles = new int[vertexCount * 2 * 3 * 4];
        }

        public void AddVertex(Vector3 vertex, Vector2 uv, int vertexIndex)
        {
            if (vertexIndex < 0)
            {
                borderVertices[-vertexIndex - 1] = vertex;
            }
            else
            {
                vertices[vertexIndex] = vertex;
                uvs[vertexIndex] = uv;
            }
        }

        int triangleIndex = 0;
        int borderTriangleIndex = 0;
        public void AddTriangle(int a, int b, int c)
        {
            if (a < 0 || b < 0 || c < 0)
            {
                borderTriangles[borderTriangleIndex] = a;
                borderTriangles[borderTriangleIndex + 1] = b;
                borderTriangles[borderTriangleIndex + 2] = c;
                borderTriangleIndex += 3;
            }
            else
            {
                triangles[triangleIndex] = a;
                triangles[triangleIndex + 1] = b;
                triangles[triangleIndex + 2] = c;
                triangleIndex += 3;
            }
        }

        public void RecalculateNormals()
        {
            // TODO Shadows look like blocks (I think vertices has be aligned in a square format)

            if (!WorldTerrain.isPlaying)
            {
                WorldTerrain.normals.Add(new List<Vector3[]>());
            }

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int vertexIndexA = triangles[i];
                int vertexIndexB = triangles[i + 1];
                int vertexIndexC = triangles[i + 2];

                Vector3 triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC);
                normals[vertexIndexA] += triangleNormal;
                normals[vertexIndexB] += triangleNormal;
                normals[vertexIndexC] += triangleNormal;
            }

            for (int i = 0; i < borderTriangles.Length; i += 3)
            {
                int vertexIndexA = borderTriangles[i];
                int vertexIndexB = borderTriangles[i + 1];
                int vertexIndexC = borderTriangles[i + 2];

                Vector3 triangleNormal = SurfaceNormal(vertexIndexA, vertexIndexB, vertexIndexC);
                if (vertexIndexA >= 0)
                {
                    normals[vertexIndexA] += triangleNormal;
                }
                if (vertexIndexB >= 0)
                {
                    normals[vertexIndexB] += triangleNormal;
                }
                if (vertexIndexC >= 0)
                {
                    normals[vertexIndexC] += triangleNormal;
                }
            }

            for (int i = 0; i < normals.Length; ++i)
            {
                normals[i].Normalize();
            }

            if (!WorldTerrain.isPlaying)
            {
                for (int i = 0; i < normals.Length; ++i)
                {
                    WorldTerrain.normals.Last().Add(new Vector3[] { normals[i] * 4 + vertices[i], vertices[i] });
                }
            }
        }

        Vector3 SurfaceNormal(int a, int b, int c)
        {
            Vector3 pointA = a < 0 ? borderVertices[-a - 1] : vertices[a];
            Vector3 pointB = b < 0 ? borderVertices[-b - 1] : vertices[b];
            Vector3 pointC = c < 0 ? borderVertices[-c - 1] : vertices[c];

            if (!WorldTerrain.isPlaying)
            {
                if (a < 0 || b < 0 || c < 0)
                    WorldTerrain.borderTriangles.Last().Add(new Vector3[3, 2]
                    {
                        { pointA, pointB },
                        { pointB, pointC },
                        { pointC, pointA }
                    });
            }

            Vector3 sideAB = pointB - pointA;
            Vector3 sideAC = pointC - pointA;

            return Vector3.Cross(sideAB, sideAC).normalized;
        }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.normals = normals;

            return mesh;
        }
    }
}