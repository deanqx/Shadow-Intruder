using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Terrain
{
    [System.Serializable]
    public struct LODPreset
    {
        [Range(0, 6)]
        public int lod;
        public float viewDistance;
        public bool Collider;
    }

    public class WorldTerrain : MonoBehaviour
    {
        public static bool isPlaying;

        const float playerMoveThreshold = 25f;
        const float sqrPlayerMoveThreshold = playerMoveThreshold * playerMoveThreshold;

        public bool updateEveryFrame;
        [Range(0, 6)]
        public int colliderLOD;

        public LODPreset[] detailLevels;

        public Transform player;
        public Material mapMaterial;

        MapGenerator overall;
        MapData mapData;

        Chunk[,] chunks;
        int mapVertices;
        int chunkCount;

        Vector2 previousPlayerPos;
        void Update()
        {
            Vector2 pos = new Vector2(player.position.x, player.position.z) / overall.scale;
            bool generated = true;

            foreach (Chunk c in chunks)
                if (!c.generated)
                {
                    generated = false;
                    break;
                }

            if (generated && ((previousPlayerPos - pos).sqrMagnitude > sqrPlayerMoveThreshold || updateEveryFrame))
            {
                previousPlayerPos = pos;

                UpdateChunks(mapData, pos);
            }
        }

        public bool onlyFirstGizmos;
        public static List<List<Vector3[]>> normals;
        public static List<List<Vector3[,]>> borderTriangles;
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            if (chunks != null && chunks.Length > 0)
                foreach (Chunk c in chunks)
                {
                    Gizmos.DrawWireCube(c.bounds.center * overall.scale, c.bounds.size * overall.scale);
                }

            Vector3 offset1 = Vector3.zero;
            if (borderTriangles != null)
                for (int i = 0; i < borderTriangles.Count; ++i)
                {
                    if (i % 4 == 0)
                    {
                        Gizmos.color = Color.magenta;
                    }
                    else if (onlyFirstGizmos)
                    {
                        continue;
                    }
                    else
                    {
                        if (i % 4 == 1)
                        {
                            // Gizmos.color = Color.green;
                        }
                        else if (i % 4 == 2)
                        {
                            // Gizmos.color = Color.black;
                            offset1.z = MapGenerator.chunkSize * 2;
                        }
                        else if (i % 4 == 3)
                        {
                            // Gizmos.color = Color.yellow;
                            offset1.z = MapGenerator.chunkSize * 2;
                        }
                    }

                    if (borderTriangles[i] != null && borderTriangles[i].Count < 1000)
                        foreach (Vector3[,] tri in borderTriangles[i])
                        {
                            Gizmos.DrawLine(tri[0, 0] - offset1, tri[0, 1] - offset1);
                            Gizmos.DrawLine(tri[1, 0] - offset1, tri[1, 1] - offset1);
                            Gizmos.DrawLine(tri[2, 0] - offset1, tri[2, 1] - offset1);
                        }
                }

            Vector3 offset2 = Vector3.zero;
            if (normals != null)
                for (int i = 0; i < normals.Count; ++i)
                {
                    if (i % 4 == 0)
                    {
                        Gizmos.color = Color.white;
                    }
                    else if (onlyFirstGizmos)
                    {
                        continue;
                    }
                    else
                    {
                        if (i % 4 == 1)
                        {
                            Gizmos.color = Color.green;
                        }
                        else if (i % 4 == 2)
                        {
                            Gizmos.color = Color.black;
                            offset2.z = MapGenerator.chunkSize * 2;
                        }
                        else if (i % 4 == 3)
                        {
                            Gizmos.color = Color.yellow;
                            offset2.z = MapGenerator.chunkSize * 2;
                        }
                    }

                    if (normals[i] != null && normals[i].Count < 1000)
                        foreach (Vector3[] normal in normals[i])
                            Gizmos.DrawLine(normal[0] - offset2, normal[1] - offset2);
                }
        }

        public void GenerateMeshData(MapGenerator overall, NoiseData noiseData)
        {
            this.overall = overall;

            chunkCount = (int)((float)overall.worldSize / overall.scale) / MapGenerator.chunkVertices;
            mapVertices = chunkCount * MapGenerator.chunkVertices;

            Vector2 pos = new Vector2(player.position.x, player.position.z) / overall.scale;

            mapData = new MapData(overall.seed, noiseData, mapVertices, mapVertices);

            chunks = new Chunk[chunkCount, chunkCount];
            for (int y = 0; y < chunkCount; ++y)
            {
                for (int x = 0; x < chunkCount; ++x)
                {
                    int chunkOffsetX = x * MapGenerator.chunkSize;
                    int chunkOffsetY = y * MapGenerator.chunkSize;

                    Vector3 center = new Vector3((float)MapGenerator.chunkSize / 2f + (float)chunkOffsetX, 0, (float)MapGenerator.chunkSize / -2f - (float)chunkOffsetY);
                    Vector3 size = new Vector3(MapGenerator.chunkSize, 2f * noiseData.meshHeightMultiplier, MapGenerator.chunkSize);

                    chunks[x, y] = new Chunk(this, x, y, chunkOffsetX, chunkOffsetY, new Bounds(center, size));
                }
            }

            new Thread(() =>
            {
                for (int y = 0; y < chunkCount; ++y)
                {
                    for (int x = 0; x < chunkCount; ++x)
                    {
                        chunks[x, y].RequestMeshData(mapData);
                    }
                }
            }).Start();

            for (int y = 0; y < chunkCount; ++y)
            {
                for (int x = 0; x < chunkCount; ++x)
                {
                    chunks[x, y].meshRenderer.material.mainTexture = TextureGenerator.TextureFromColorMap(mapData, chunks[x, y].chunkOffsetX, chunks[x, y].chunkOffsetY, MapGenerator.chunkSize, MapGenerator.chunkSize);
                }
            }
        }

        public void UpdateChunks(MapData mapData, Vector2 pos)
        {
            int width = chunks.GetLength(0);
            int height = chunks.GetLength(1);

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Chunk c = chunks[x, y];

                    float distance = c.GetDistance(pos);
                    int lastLOD = c.lastLOD;
                    int lod = c.GetLOD(distance, detailLevels);

                    if (lod != lastLOD || updateEveryFrame)
                    {
                        int clod = lod + colliderLOD > 6 ? 6 : lod + colliderLOD;

                        if (c.meshLODs[lod] == null)
                            c.meshLODs[lod] = c.meshDataLODs[lod].CreateMesh();

                        if (c.meshLODs[clod] == null)
                            c.meshLODs[clod] = c.meshDataLODs[clod].CreateMesh();

                        c.meshFilter.mesh = c.meshLODs[lod];

                        if (c.collider)
                            c.meshCollider.sharedMesh = c.meshLODs[clod];
                        else
                            c.meshCollider.sharedMesh = null;
                    }
                }
            }
        }

        class Chunk
        {
            public readonly int x;
            public readonly int y;
            public readonly int chunkOffsetX;
            public readonly int chunkOffsetY;
            public readonly Bounds bounds;

            public float lastPlayerDistance = -1;
            public int lastLOD = -1;
            public bool generated = false;
            public bool collider = false;

            public MeshData[] meshDataLODs = new MeshData[7];
            public Mesh[] meshLODs = new Mesh[7];

            public GameObject meshObject;
            public MeshRenderer meshRenderer;
            public MeshFilter meshFilter;
            public MeshCollider meshCollider;

            public Chunk(WorldTerrain p, int x, int y, int chunkOffsetX, int chunkOffsetY, Bounds bounds)
            {
                this.x = x;
                this.y = y;
                this.chunkOffsetX = chunkOffsetX;
                this.chunkOffsetY = chunkOffsetY;
                this.bounds = bounds;

                meshObject = new GameObject("Terrain Chunk");
                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshRenderer = meshObject.AddComponent<MeshRenderer>();
                meshCollider = meshObject.AddComponent<MeshCollider>();
                meshRenderer.material = p.mapMaterial;

                meshObject.transform.localScale = Vector3.one * p.overall.scale;
                meshObject.transform.parent = p.overall.parent;
            }

            public void RequestMeshData(MapData mapData)
            {
                Task.Run(() =>
                {
                    generated = false;
                    for (int i = 0; i < meshDataLODs.Length; ++i)
                    {
                        meshDataLODs[i] = mapData.GenerateMeshData(chunkOffsetX, chunkOffsetY, i);
                    }
                    generated = true;
                });
            }
            public void oldRequestMeshData(MapData mapData)
            {
                new Thread(() =>
                {
                    generated = false;
                    for (int i = 0; i < meshDataLODs.Length; ++i)
                    {
                        meshDataLODs[i] = mapData.GenerateMeshData(chunkOffsetX, chunkOffsetY, i);
                    }
                    generated = true;
                }).Start();
            }

            public float GetDistance(Vector2 pos)
            {
                float distance = Mathf.Sqrt(bounds.SqrDistance(new Vector3(pos.x, 0, pos.y)));

                lastPlayerDistance = distance;
                return distance;
            }

            public int GetLOD(float playerDistance, LODPreset[] detailLevels)
            {
                int lod = 6;
                bool collider = false;

                for (int i = 0; i < detailLevels.Length; ++i)
                {
                    if (playerDistance <= detailLevels[i].viewDistance)
                    {
                        lod = detailLevels[i].lod;
                        collider = detailLevels[i].Collider;
                        break;
                    }
                }

                lastLOD = lod;
                this.collider = collider;
                return lod;
            }
        }
    }
}