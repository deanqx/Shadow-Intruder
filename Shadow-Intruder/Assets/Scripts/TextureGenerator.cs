using System;
using UnityEngine;

namespace Terrain
{
    public static class TextureGenerator
    {
        public static Texture2D TextureFromColorMap(Color[] colorMap, int verticesX, int offsetX, int offsetY, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] offsetColorMap = new Color[width * height];
            for (int y = 0; y < height; ++y)
            {
                Array.Copy(colorMap, (y + offsetY) * verticesX + offsetX, offsetColorMap, y * width, width);
            }

            texture.SetPixels(offsetColorMap);
            texture.Apply();

            return texture;
        }

        public static Texture2D TextureFromHeightMap(float[,] heightMap, int verticesX, int verticesY, int offsetX = 0, int offsetY = 0)
        {
            Color[] colorMap = new Color[verticesX * verticesY];

            for (int y = 0; y < verticesY; ++y)
            {
                for (int x = 0; x < verticesX; ++x)
                {
                    colorMap[y * verticesX + x] = Color.Lerp(Color.white, Color.black, heightMap[x, y]);
                }
            }

            // WARN Used MapGenerator.chunkSize instead of verticesX
            return TextureFromColorMap(colorMap, verticesX, offsetX, offsetY, MapGenerator.chunkSize, MapGenerator.chunkSize);
        }
    }
}