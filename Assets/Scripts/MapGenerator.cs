using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

[System.Serializable]
public struct TerrainType
{
    public string name;
    [Range(0, 1)]
    public float height;
    public Color color;
}

public class MapGenerator : MonoBehaviour
{
    public int worldSize;
    public int seed;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public Transform parent;

    public GameObject preview0Mesh;
    public GameObject preview1Mesh;
    public GameObject preview2Mesh;
    public GameObject preview3Mesh;
    public enum PreviewMode { NoiseMap, ColorMap, HeightMapSolo, HeightMap };
    public PreviewMode previewMode;

    public const int chunkSize = 240;
    public const int chunkVertices = 241;
    [Range(0, 6)]
    public int previewLOD;
    public float noiseScale;
    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public TerrainType[] regions;

    public bool autoUpdate;
    // TEMP borderNormals
    public bool borderNormals;

    public void Start()
    {
        WorldTerrain.isPlaying = true;

        preview0Mesh.SetActive(false);
        preview1Mesh.SetActive(false);
        preview2Mesh.SetActive(false);
        preview3Mesh.SetActive(false);

        WorldTerrain t = FindObjectOfType<WorldTerrain>();
        t.GenerateMeshData(this);
    }

    public void DrawTexture(Texture2D texture)
    {
        preview0Mesh.GetComponent<MeshFilter>().sharedMesh = new MapData(this, chunkVertices, chunkVertices).GenerateMeshData(0, 0, previewLOD, borderNormals).CreateMesh();
        preview0Mesh.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
    }

    public void DrawMesh(GameObject obj, MeshData meshData, Texture2D texture)
    {
        obj.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
        obj.GetComponent<MeshFilter>().sharedMesh = meshData.CreateMesh();
    }

    public void DrawMapInEditor()
    {
        WorldTerrain.isPlaying = false;

        WorldTerrain.normals = new List<List<Vector3[]>>();
        WorldTerrain.borderTriangles = new List<List<Vector3[,]>>();

        MapData mapData = new MapData(this, chunkVertices * 2, chunkVertices * 2);

        preview0Mesh.SetActive(false);
        preview1Mesh.SetActive(false);
        preview2Mesh.SetActive(false);
        preview3Mesh.SetActive(false);

        if (previewMode == PreviewMode.NoiseMap)
        {
            preview0Mesh.SetActive(true);

            DrawTexture(TextureGenerator.TextureFromHeightMap(mapData));
        }
        else if (previewMode == PreviewMode.ColorMap)
        {
            preview0Mesh.SetActive(true);
            
            DrawTexture(TextureGenerator.TextureFromColorMap(mapData, 0, 0, chunkSize, chunkSize));
        }
        else if (previewMode == PreviewMode.HeightMapSolo)
        {
            preview0Mesh.SetActive(true);

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData, 0, 0, chunkSize, chunkSize);
            DrawMesh(preview0Mesh, mapData.GenerateMeshData(0, 0, previewLOD, borderNormals), texture);
        }
        else if (previewMode == PreviewMode.HeightMap)
        {
            preview0Mesh.SetActive(true);
            preview1Mesh.SetActive(true);
            preview2Mesh.SetActive(true);
            preview3Mesh.SetActive(true);

            // WARN offset: 1 -> 0
            Texture2D texture0 = TextureGenerator.TextureFromColorMap(mapData, 0, 0, chunkSize, chunkSize);
            DrawMesh(preview0Mesh, mapData.GenerateMeshData(0, 0, previewLOD, borderNormals), texture0);

            Texture2D texture1 = TextureGenerator.TextureFromColorMap(mapData, chunkSize, 0, chunkSize, chunkSize);
            DrawMesh(preview1Mesh, mapData.GenerateMeshData(chunkSize, 0, previewLOD, borderNormals), texture1);

            Texture2D texture2 = TextureGenerator.TextureFromColorMap(mapData, 0, chunkSize, chunkSize, chunkSize);
            DrawMesh(preview2Mesh, mapData.GenerateMeshData(0, chunkSize, 6, borderNormals), texture2);

            Texture2D texture3 = TextureGenerator.TextureFromColorMap(mapData, chunkSize, chunkSize, chunkSize, chunkSize);
            DrawMesh(preview3Mesh, mapData.GenerateMeshData(chunkSize, chunkSize, 6, borderNormals), texture3);
        }
    }

    void OnValidate()
    {
        if (octaves < 0)
            octaves = 0;
        if (lacunarity < 1f)
            lacunarity = 1f;
    }
}