using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Terrain
{
    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        [Range(0, 1)]
        public float height;
        public Color color;
    }

    [System.Serializable]
    public struct Preview
    {
        public GameObject mesh0;
        public GameObject mesh1;
        public GameObject mesh2;
        public GameObject mesh3;
        public enum Mode { NoiseMap, ColorMap, HeightMapSolo, HeightMap };
        public Mode mode;
        [Range(0, 6)]
        public int LOD;
    }

    public class MapGenerator : MonoBehaviour
    {
        public const int chunkSize = 240;
        public const int chunkVertices = 241;

        public int worldSize;
        [Range(0, 2)]
        public float scale = 1f;
        public int seed;

        public int octaves;
        [Range(0, 1)]
        public float persistance;
        public float lacunarity;

        public float noiseScale;
        public Vector2 offset;

        public float meshHeightMultiplier;
        public AnimationCurve meshHeightCurve;

        public Transform parent;

        public TerrainType[] regions;

        public Preview preview;

        public bool autoUpdate;

        public void Start()
        {
            WorldTerrain.isPlaying = true;

            preview.mesh0.SetActive(false);
            preview.mesh1.SetActive(false);
            preview.mesh2.SetActive(false);
            preview.mesh3.SetActive(false);

            WorldTerrain t = FindObjectOfType<WorldTerrain>();
            t.GenerateMeshData(this);
        }

        public void DrawTexture(Texture2D texture)
        {
            preview.mesh0.GetComponent<MeshFilter>().sharedMesh = new MapData(this, chunkVertices, chunkVertices).GenerateMeshData(0, 0, preview.LOD).CreateMesh();
            preview.mesh0.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
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

            preview.mesh0.SetActive(false);
            preview.mesh1.SetActive(false);
            preview.mesh2.SetActive(false);
            preview.mesh3.SetActive(false);

            preview.mesh0.transform.localScale = Vector3.one * scale;
            preview.mesh1.transform.localScale = Vector3.one * scale;
            preview.mesh2.transform.localScale = Vector3.one * scale;
            preview.mesh3.transform.localScale = Vector3.one * scale;

            if (preview.mode == Preview.Mode.NoiseMap)
            {
                preview.mesh0.SetActive(true);

                DrawTexture(TextureGenerator.TextureFromHeightMap(mapData));
            }
            else if (preview.mode == Preview.Mode.ColorMap)
            {
                preview.mesh0.SetActive(true);

                DrawTexture(TextureGenerator.TextureFromColorMap(mapData, 0, 0, chunkSize, chunkSize));
            }
            else if (preview.mode == Preview.Mode.HeightMapSolo)
            {
                preview.mesh0.SetActive(true);

                Texture2D texture = TextureGenerator.TextureFromColorMap(mapData, 0, 0, chunkSize, chunkSize);
                DrawMesh(preview.mesh0, mapData.GenerateMeshData(0, 0, preview.LOD), texture);
            }
            else if (preview.mode == Preview.Mode.HeightMap)
            {
                preview.mesh0.SetActive(true);
                preview.mesh1.SetActive(true);
                preview.mesh2.SetActive(true);
                preview.mesh3.SetActive(true);

                preview.mesh2.transform.position = new Vector3(0f, 0f, chunkSize * -2f) * scale;
                preview.mesh3.transform.position = new Vector3(0f, 0f, chunkSize * -2f) * scale;

                // WARN offset: 1 -> 0
                Texture2D texture0 = TextureGenerator.TextureFromColorMap(mapData, 0, 0, chunkSize, chunkSize);
                DrawMesh(preview.mesh0, mapData.GenerateMeshData(0, 0, preview.LOD), texture0);

                Texture2D texture1 = TextureGenerator.TextureFromColorMap(mapData, chunkSize, 0, chunkSize, chunkSize);
                DrawMesh(preview.mesh1, mapData.GenerateMeshData(chunkSize, 0, preview.LOD), texture1);

                Texture2D texture2 = TextureGenerator.TextureFromColorMap(mapData, 0, chunkSize, chunkSize, chunkSize);
                DrawMesh(preview.mesh2, mapData.GenerateMeshData(0, chunkSize, 6), texture2);

                Texture2D texture3 = TextureGenerator.TextureFromColorMap(mapData, chunkSize, chunkSize, chunkSize, chunkSize);
                DrawMesh(preview.mesh3, mapData.GenerateMeshData(chunkSize, chunkSize, 6), texture3);
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
}