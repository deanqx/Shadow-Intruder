using UnityEngine;
using System.Collections.Generic;

namespace Terrain
{
    public class MapData
    {
        Noise noise;

        public float[,] heightMap;
        public Color[] colorMap;

        public int verticesX;
        public int verticesY;
        public int width;
        public int height;

        public MapData(int seed, Noise noise, int verticesX, int verticesY)
        {
            if (seed < 0)
                seed = new System.Random().Next(0, 9999);

            this.noise = noise;
            this.verticesX = verticesX;
            this.verticesY = verticesY;
            width = verticesX + 23; // WARN 23 = 24(border vertices) - 1
            height = verticesY + 23;

            heightMap = new float[width, height];
            colorMap = new Color[width * height];

            // PERF Generate noise for every chunk (Mesh generation will break)
            heightMap = noise.GenerateNoiseMap(width, height, seed);

            for (int y = 0; y < verticesY; ++y)
            {
                for (int x = 0; x < verticesX; ++x)
                {
                    float currentHeight = heightMap[x + 12, y + 12];
                    for (int i = 0; i < noise.regions.Length; ++i)
                    {
                        if (currentHeight <= noise.regions[i].height)
                        {
                            colorMap[y * verticesX + x] = noise.regions[i].color;
                            break;
                        }
                    }
                }
            }
        }

        public MeshData GenerateMeshData(int offsetX, int offsetY, int lod)
        {
            if (!WorldTerrain.isPlaying)
            {
                WorldTerrain.borderTriangles.Add(new List<Vector3[,]>());
            }

            AnimationCurve _meshHeightCurve = new AnimationCurve(noise.meshHeightCurve.keys);

            // TODO Optional: Add division by average
            int division = (lod == 0) ? 1 : lod * 2;

            int vertexCount = MapGenerator.chunkSize / division + 1;
            int borderedSize = vertexCount + 2;

            MeshData meshData = new MeshData(vertexCount);

            int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
            int meshVertexIndex = 0;
            int borderVertexIndex = -1;
            for (int by = 0; by < borderedSize; ++by)
            {
                for (int bx = 0; bx < borderedSize; ++bx)
                {
                    bool isBorderVertex = by == 0 || by == borderedSize - 1 || bx == 0 || bx == borderedSize - 1;

                    if (isBorderVertex)
                    {
                        vertexIndicesMap[bx, by] = borderVertexIndex;
                        --borderVertexIndex;
                    }
                    else
                    {
                        vertexIndicesMap[bx, by] = meshVertexIndex;
                        ++meshVertexIndex;
                    }
                }
            }

            for (int by = 0; by < borderedSize; ++by)
            {
                for (int bx = 0; bx < borderedSize; ++bx)
                {
                    int index = vertexIndicesMap[bx, by];

                    Vector2 local;
                    Vector2 uv;
                    if (index < 0)
                    {
                        local = new Vector2(bx * division - division, by * division - division);
                        uv = new Vector2();
                    }
                    else
                    {
                        int x = bx - 1;
                        int y = by - 1;

                        local = new Vector2(x * division, y * division);
                        uv = new Vector2((float)x / (float)(vertexCount - 1), (float)y / (float)(vertexCount - 1));
                    }

                    int mapX = (int)local.x + offsetX + 12;
                    int mapY = (int)local.y + offsetY + 12;
                    float height = _meshHeightCurve.Evaluate(heightMap[mapX, mapY]) * noise.meshHeightMultiplier;

                    Vector3 global = new Vector3(offsetX + local.x, height, -offsetY - local.y);

                    meshData.AddVertex(global, uv, index);

                    if (bx + 1 < borderedSize && by + 1 < borderedSize)
                    {
                        int a = vertexIndicesMap[bx, by];
                        int b = vertexIndicesMap[bx + 1, by];
                        int c = vertexIndicesMap[bx + 1, by + 1];
                        int d = vertexIndicesMap[bx, by + 1];

                        meshData.AddTriangle(a, b, c);
                        meshData.AddTriangle(a, c, d);
                    }
                }
            }

            meshData.RecalculateNormals();

            return meshData;
        }
    }
}