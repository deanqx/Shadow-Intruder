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
        public Vector2 offset;
        public float meshHeightMultiplier;

        public AnimationCurve meshHeightCurve;
        public NoiseLayer[] noiseLayers;
        public TerrainType[] regions;

        protected override void OnValidate()
        {
            base.OnValidate();
        }

        static float Evaluate(float value, float a, float b)
        {
            return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
        }

        void GenerateFalloffMap(ref float[,] noiseMap, int verticesX, int verticesY)
        {
            for (int y = 0; y < verticesY; ++y)
            {
                for (int x = 0; x < verticesX; ++x)
                {
                    float j = x / (float)verticesX * 2 - 1;
                    float k = y / (float)verticesY * 2 - 1;

                    noiseMap[x, y] -= Evaluate(Mathf.Max(Mathf.Abs(j), Mathf.Abs(k)), falloffTransition, falloffBias);
                }
            }
        }

        public float[,] GenerateNoiseMap(int width, int height, int seed, int taskCount = 32)
        {
            float[,] noiseMap = new float[width, height];

            for (int i = 0; i < noiseLayers.Length; ++i)
            {
                noiseMap = noiseLayers[i].Generate(noiseMap, width, height, offset, seed, taskCount);
            }

            float maxHeight = float.MinValue;
            float minHeight = float.MaxValue;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (noiseMap[x, y] > maxHeight)
                        maxHeight = noiseMap[x, y];
                    else if (noiseMap[x, y] < minHeight)
                        minHeight = noiseMap[x, y];
                }
            }

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x, y]);
                }
            }

            if (falloff)
            {
                GenerateFalloffMap(ref noiseMap, width, height);
            }

            return noiseMap;
        }
    }

    [System.Serializable]
    public class NoiseLayer
    {
        public enum NoiseType { Perlin, Rigid };
        public NoiseType noiseType;
        [Range(0.1f, 2f)]
        public float strenght = 1;
        public bool enabled;

        [Space(15)]

        public int octaves;
        [Range(0f, 1f)]
        public float persistance;
        [Range(1f, 1.5f)]
        public float lacunarity;

        public float noiseScale;

        public NoiseLayer()
        {
            if (octaves < 1)
                octaves = 1;
            if (lacunarity < 1f)
                lacunarity = 1f;
            if (noiseScale < 1)
                noiseScale = 1f;
        }

        public float[,] Generate(float[,] noiseMap, int width, int height, Vector2 offset, int seed, int taskCount = 32)
        {
            if (enabled)
            {
                Func<float, float, float> noiseFunc = PerlinNoise;
                if (noiseType == NoiseType.Rigid)
                    noiseFunc = RigidNoise;

                System.Random rng = new System.Random(seed);
                Vector2[] octaveOffsets = new Vector2[octaves];
                for (int i = 0; i < octaves; ++i)
                {
                    float offsetX = rng.Next(-100000, 100000) + offset.x;
                    float offsetY = rng.Next(-100000, 100000) + offset.y;
                    octaveOffsets[i] = new Vector2(offsetX, offsetY);
                }

                Task[] tasks = new Task[taskCount];

                int linesPerTask = height / taskCount;

                for (int t = 0; t < taskCount; ++t)
                {
                    int _t = t;

                    tasks[_t] = Task.Run(() =>
                    {
                        RequestNoiseMap(noiseFunc, ref noiseMap, offsetY: linesPerTask * _t, linesPerTask, width, octaveOffsets);
                    });
                }

                int linesRemaining = height - (linesPerTask * taskCount);

                RequestNoiseMap(noiseFunc, ref noiseMap, offsetY: linesPerTask * taskCount, linesRemaining, width, octaveOffsets);

                for (int t = 0; t < taskCount; ++t)
                {
                    tasks[t].Wait();
                }
            }

            return noiseMap;
        }

        float PerlinNoise(float sampleX, float sampleY)
        {
            // WARN return Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
            return Mathf.PerlinNoise(sampleX, sampleY);
        }

        float RigidNoise(float sampleX, float sampleY)
        {
            float v = 1f - Mathf.Abs(Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1);
            return v * v;
        }

        void RequestNoiseMap(Func<float, float, float> noiseFunc, ref float[,] noiseMap, int offsetY, int lenghtY, int width, Vector2[] octaveOffsets)
        {
            for (int y = 0; y < lenghtY; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; ++i)
                    {
                        float sampleX = (x + octaveOffsets[i].x) / noiseScale * frequency;
                        float sampleY = (y + offsetY + octaveOffsets[i].y) / noiseScale * frequency;

                        float perlinValue = noiseFunc(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
                    }

                    noiseMap[x, y + offsetY] += noiseHeight * strenght;
                }
            }
        }
    }
}