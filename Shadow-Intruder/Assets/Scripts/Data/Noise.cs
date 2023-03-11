using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;

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

    [CreateAssetMenu]
    public class Noise : UpdateData
    {
        public bool falloff;
        public float falloffTransition;
        public float falloffBias;

        public int octaves;
        [Range(0, 1)]
        public float persistance;
        [Range(1f, 1.5f)]
        public float lacunarity;

        public float noiseScale;
        public Vector2 offset;

        public float meshHeightMultiplier;
        public AnimationCurve meshHeightCurve;

        public TerrainType[] regions;

        protected override void OnValidate()
        {
            if (octaves < 0)
                octaves = 0;
            if (lacunarity < 1f)
                lacunarity = 1f;
            if (noiseScale <= 0)
                noiseScale = 0.0001f;

            base.OnValidate();
        }

        static float Evaluate(float value, float a, float b)
        {
            return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
        }

        public float[,] GenerateFalloffMap(int verticesX, int verticesY)
        {
            float[,] map = new float[verticesX, verticesY];

            for (int y = 0; y < verticesY; ++y)
            {
                for (int x = 0; x < verticesX; ++x)
                {
                    float j = x / (float)verticesX * 2 - 1;
                    float k = y / (float)verticesY * 2 - 1;

                    map[x, y] = Evaluate(Mathf.Max(Mathf.Abs(j), Mathf.Abs(k)), falloffTransition, falloffBias);
                }
            }

            return map;
        }

        public float[,] GenerateNoiseMap(int width, int height, int seed)
        {
            float[,] noiseMap = new float[width, height];

            System.Random rng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; ++i)
            {
                float offsetX = rng.Next(-100000, 100000) + offset.x;
                float offsetY = rng.Next(-100000, 100000) - offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; ++i)
                    {
                        float sampleX = (x + octaveOffsets[i].x) / noiseScale * frequency;
                        float sampleY = (y + octaveOffsets[i].y) / noiseScale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    if (noiseHeight > maxHeight)
                        maxHeight = noiseHeight;
                    else if (noiseHeight < minHeight)
                        minHeight = noiseHeight;

                    noiseMap[x, y] = noiseHeight;
                }
            }

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }

        public float[,] FastGenerateNoiseMap(int width, int height, int seed, int taskCount = 32)
        {
            System.Random rng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];
            for (int i = 0; i < octaves; ++i)
            {
                float offsetX = rng.Next(-100000, 100000) + offset.x;
                float offsetY = rng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            float[,] noiseMap = new float[width, height];
            Task[] tasks = new Task[taskCount];
            RequestNoise[] requests = new RequestNoise[taskCount];

            int linesPerTask = height / taskCount;

            for (int t = 0; t < taskCount; ++t)
            {
                int _t = t;

                tasks[_t] = Task.Run(() =>
                {
                    requests[_t] = new RequestNoise(this);
                    requests[_t].RequestNoiseMap(ref noiseMap, offsetY: linesPerTask * _t, linesPerTask, width, octaveOffsets);
                });
            }

            int linesRemaining = height - (linesPerTask * taskCount);

            RequestNoise remaining = new RequestNoise(this);
            remaining.RequestNoiseMap(ref noiseMap, offsetY: linesPerTask * taskCount, linesRemaining, width, octaveOffsets);

            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;

            if (maxHeight < remaining.maxHeight)
                maxHeight = remaining.maxHeight;
            if (minHeight > remaining.minHeight)
                minHeight = remaining.minHeight;

            for (int t = 0; t < taskCount; ++t)
            {
                tasks[t].Wait();

                if (maxHeight < requests[t].maxHeight)
                    maxHeight = requests[t].maxHeight;
                if (minHeight > requests[t].minHeight)
                    minHeight = requests[t].minHeight;
            }

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x, y]);
                }
            }

            return noiseMap;
        }
    }

    class RequestNoise
    {
        Noise noise;
        public float maxHeight;
        public float minHeight;

        public RequestNoise(Noise noise)
        {
            this.noise = noise;
            maxHeight = float.MinValue;
            minHeight = float.MaxValue;
        }

        public void RequestNoiseMap(ref float[,] noiseMap, int offsetY, int lenghtY, int width, Vector2[] octaveOffsets)
        {
            for (int y = 0; y < lenghtY; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < noise.octaves; ++i)
                    {
                        float sampleX = (x + octaveOffsets[i].x) / noise.noiseScale * frequency;
                        float sampleY = (y + offsetY + octaveOffsets[i].y) / noise.noiseScale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= noise.persistance;
                        frequency *= noise.lacunarity;
                    }

                    if (noiseHeight > maxHeight)
                        maxHeight = noiseHeight;
                    else if (noiseHeight < minHeight)
                        minHeight = noiseHeight;

                    noiseMap[x, y + offsetY] = noiseHeight;
                }
            }
        }
    }
}