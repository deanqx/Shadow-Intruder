using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Terrain
{
    public static class Noise
    {
        public static float[,] GenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
        {
            if (scale <= 0)
                scale = 0.0001f;

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

            // WARN Why needed?
            // float halfWidth = width / 2f;
            // float halfHeight = height / 2f;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < octaves; ++i)
                    {
                        float sampleX = (x + octaveOffsets[i].x) / scale * frequency;
                        float sampleY = (y + octaveOffsets[i].y) / scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                        // float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
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

        public static float[,] FastGenerateNoiseMap(int width, int height, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, int taskCount = 32)
        {
            if (scale <= 0)
                scale = 0.0001f;

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
                    requests[_t] = new RequestNoise();
                    requests[_t].RequestNoiseMap(ref noiseMap, offsetY: linesPerTask * _t, linesPerTask, width, octaves, octaveOffsets, scale, persistance, lacunarity);
                });
            }

            int linesRemaining = height - (linesPerTask * taskCount);

            RequestNoise remaining = new RequestNoise();
            remaining.RequestNoiseMap(ref noiseMap, offsetY: linesPerTask * taskCount, linesRemaining, width, octaves, octaveOffsets, scale, persistance, lacunarity);

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
        public float maxHeight;
        public float minHeight;

        public RequestNoise()
        {
            maxHeight = float.MinValue;
            minHeight = float.MaxValue;
        }

        public void RequestNoiseMap(ref float[,] noiseMap, int offsetY, int lenghtY, int width, int octaves, Vector2[] octaveOffsets, float scale, float persistance, float lacunarity)
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
                        float sampleX = (x + octaveOffsets[i].x) / scale * frequency;
                        float sampleY = (y + offsetY + octaveOffsets[i].y) / scale * frequency;

                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                        noiseHeight += perlinValue * amplitude;

                        amplitude *= persistance;
                        frequency *= lacunarity;
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