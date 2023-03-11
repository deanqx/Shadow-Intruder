using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;

namespace Terrain
{
    [System.Serializable]
    public struct Preview
    {
        public GameObject mesh0;
        public GameObject mesh1;
        public GameObject mesh2;
        public GameObject mesh3;
        public enum Mode { NoiseMap, ColorMap, HeightMapSolo, HeightMap, FalloffMap };
        public Mode mode;
        [Range(0, 6)]
        public int LOD1;
        [Range(0, 6)]
        public int LOD2;
    }

    public class MapGenerator : MonoBehaviour
    {
        public const int chunkSize = 240;
        public const int chunkVertices = 241;

        public int worldSize;
        public int seed;
        [Range(0, 2)]
        public float scale = 1f;

        public Noise noisePreset;

        public Transform parent;
        public Preview preview;

        public bool autoUpdate;

        void OnValuesUpdated()
        {
            if (!Application.isPlaying)
            {
                DrawMapInEditor();
            }
        }

        void OnValidate()
        {
            if (noisePreset != null)
            {
                noisePreset.OnValuesUpdated -= OnValuesUpdated;
                noisePreset.OnValuesUpdated += OnValuesUpdated;
            }
        }

        public void Start()
        {
            WorldTerrain.isPlaying = true;

            preview.mesh0.SetActive(false);
            preview.mesh1.SetActive(false);
            preview.mesh2.SetActive(false);
            preview.mesh3.SetActive(false);

            WorldTerrain t = FindObjectOfType<WorldTerrain>();
            t.GenerateMeshData(this, noisePreset);
        }

        public void DrawTexture(GameObject obj, Texture2D texture)
        {
            obj.GetComponent<MeshFilter>().sharedMesh = new MapData(seed, noisePreset, chunkVertices, chunkVertices).GenerateMeshData(0, 0, preview.LOD1).CreateMesh();
            obj.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
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

            MapData mapData = new MapData(seed, noisePreset, chunkVertices * 2, chunkVertices * 2);

            preview.mesh0.SetActive(true);
            preview.mesh1.SetActive(false);
            preview.mesh2.SetActive(false);
            preview.mesh3.SetActive(false);

            preview.mesh0.transform.localScale = Vector3.one * scale;
            preview.mesh1.transform.localScale = Vector3.one * scale;
            preview.mesh2.transform.localScale = Vector3.one * scale;
            preview.mesh3.transform.localScale = Vector3.one * scale;

            if (preview.mode == Preview.Mode.NoiseMap)
            {
                DrawTexture(preview.mesh0, TextureGenerator.TextureFromHeightMap(mapData.heightMap, mapData.verticesX, mapData.verticesY));
            }
            else if (preview.mode == Preview.Mode.ColorMap)
            {
                DrawTexture(preview.mesh0, TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.verticesX, 0, 0, chunkSize, chunkSize));
            }
            else if (preview.mode == Preview.Mode.HeightMapSolo)
            {

                Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.verticesX, 0, 0, chunkSize, chunkSize);
                DrawMesh(preview.mesh0, mapData.GenerateMeshData(0, 0, preview.LOD1), texture);
            }
            else if (preview.mode == Preview.Mode.HeightMap)
            {
                preview.mesh1.SetActive(true);
                preview.mesh2.SetActive(true);
                preview.mesh3.SetActive(true);

                // WARN offset: 1 -> 0
                Texture2D texture0 = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.verticesX, 0, 0, chunkSize, chunkSize);
                Texture2D texture1 = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.verticesX, chunkSize, 0, chunkSize, chunkSize);
                Texture2D texture2 = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.verticesX, 0, chunkSize, chunkSize, chunkSize);
                Texture2D texture3 = TextureGenerator.TextureFromColorMap(mapData.colorMap, mapData.verticesX, chunkSize, chunkSize, chunkSize, chunkSize);

                DrawMesh(preview.mesh0, mapData.GenerateMeshData(0, 0, preview.LOD1), texture0);
                DrawMesh(preview.mesh1, mapData.GenerateMeshData(chunkSize, 0, preview.LOD1), texture1);
                DrawMesh(preview.mesh2, mapData.GenerateMeshData(0, chunkSize, preview.LOD2), texture2);
                DrawMesh(preview.mesh3, mapData.GenerateMeshData(chunkSize, chunkSize, preview.LOD2), texture3);
            }
            else if (preview.mode == Preview.Mode.FalloffMap)
            {
                preview.mesh1.SetActive(true);
                preview.mesh2.SetActive(true);
                preview.mesh3.SetActive(true);

                float[,] falloff = noisePreset.GenerateFalloffMap(mapData.verticesX, mapData.verticesY);
                Texture2D texture0 = TextureGenerator.TextureFromHeightMap(falloff, mapData.verticesX, mapData.verticesY, 0, 0);
                Texture2D texture1 = TextureGenerator.TextureFromHeightMap(falloff, mapData.verticesX, mapData.verticesY, chunkSize, 0);
                Texture2D texture2 = TextureGenerator.TextureFromHeightMap(falloff, mapData.verticesX, mapData.verticesY, 0, chunkSize);
                Texture2D texture3 = TextureGenerator.TextureFromHeightMap(falloff, mapData.verticesX, mapData.verticesY, chunkSize, chunkSize);

                DrawMesh(preview.mesh0, mapData.GenerateMeshData(0, 0, preview.LOD1), texture0);
                DrawMesh(preview.mesh1, mapData.GenerateMeshData(chunkSize, 0, preview.LOD1), texture1);
                DrawMesh(preview.mesh2, mapData.GenerateMeshData(0, chunkSize, preview.LOD2), texture2);
                DrawMesh(preview.mesh3, mapData.GenerateMeshData(chunkSize, chunkSize, preview.LOD2), texture3);
            }
        }
    }
}