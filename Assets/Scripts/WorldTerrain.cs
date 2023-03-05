using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public struct LODPreset
{
    [Range(0, 6)]
    public int lod;
    public float viewDistance;
}

public class WorldTerrain : MonoBehaviour
{
    public static bool isPlaying;

    const float playerMoveThreshold = 25f;
    const float sqrPlayerMoveThreshold = playerMoveThreshold * playerMoveThreshold;

    public bool updateEveryFrame;
    public float scale = 1f;

    public LODPreset[] detailLevels;

    public Transform player;
    public Material mapMaterial;

    MapGenerator seed;
    MapData mapData;

    Chunk[,] chunks;
    int mapVertices;
    int chunkCount;

    Vector2 previousPlayerPos;
    void Update()
    {
        Vector2 pos = new Vector2(player.position.x, player.position.z) / scale;
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
                Gizmos.DrawWireCube(c.bounds.center * scale, c.bounds.size * scale);
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

    public void GenerateMeshData(MapGenerator seed)
    {
        this.seed = seed;

        chunkCount = seed.worldSize / MapGenerator.chunkVertices;
        mapVertices = chunkCount * MapGenerator.chunkVertices;

        Vector2 pos = new Vector2(player.position.x, player.position.z) / scale;

        mapData = new MapData(seed, mapVertices, mapVertices);

        chunks = new Chunk[chunkCount, chunkCount];
        for (int y = 0; y < chunkCount; ++y)
        {
            for (int x = 0; x < chunkCount; ++x)
            {
                int chunkOffsetX = x * MapGenerator.chunkSize;
                int chunkOffsetY = y * MapGenerator.chunkSize;

                Vector3 center = new Vector3((float)MapGenerator.chunkSize / 2f + (float)chunkOffsetX, 0, (float)MapGenerator.chunkSize / -2f - (float)chunkOffsetY);
                Vector3 size = new Vector3(MapGenerator.chunkSize, 2f * seed.meshHeightMultiplier, MapGenerator.chunkSize);

                chunks[x, y] = new Chunk(this, x, y, chunkOffsetX, chunkOffsetY, new Bounds(center, size));

                float distance = chunks[x, y].GetDistance(pos);
                int lod = chunks[x, y].GetLOD(distance, detailLevels);
                chunks[x, y].RequestMeshData(mapData, lod);

                chunks[x, y].meshRenderer.material.mainTexture = TextureGenerator.TextureFromColorMap(mapData, chunkOffsetX, chunkOffsetY, MapGenerator.chunkSize, MapGenerator.chunkSize);
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
                float distance = chunks[x, y].GetDistance(pos);
                int lastLOD = chunks[x, y].lastLOD;
                int lod = chunks[x, y].GetLOD(distance, detailLevels);

                if (!chunks[x, y].loaded)
                {
                    chunks[x, y].meshFilter.mesh = chunks[x, y].meshData.CreateMesh();

                    chunks[x, y].loaded = true;
                }

                if (lastLOD != lod)
                {
                    chunks[x, y].RequestMeshData(mapData, lod);
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
        public bool loaded = false;
        public bool generated = false;

        public GameObject meshObject;
        public MeshData meshData;
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;

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
            meshRenderer.material = p.mapMaterial;

            meshObject.transform.position = new Vector3(0f, 0f, chunkOffsetY * -2f) * p.scale;
            meshObject.transform.localScale = Vector3.one * p.scale;
            meshObject.transform.parent = p.seed.parent;
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
            for (int i = 0; i < detailLevels.Length; ++i)
            {
                if (playerDistance <= detailLevels[i].viewDistance)
                {
                    lod = detailLevels[i].lod;
                    break;
                }
            }

            lastLOD = lod;
            return lod;
        }

        bool requested = false;
        public void RequestMeshData(MapData mapData, int lod)
        {
            if (!requested)
            {
                requested = true;

                // TODO Add max Threadcount
                new Thread(() =>
                {
                    generated = false;
                    meshData = mapData.GenerateMeshData(chunkOffsetX, chunkOffsetY, lod, true);
                    generated = true;

                    loaded = false;
                    requested = false;
                }).Start();
            }
        }
    }
}