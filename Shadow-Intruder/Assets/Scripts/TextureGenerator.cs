using System;
using UnityEngine;

namespace Terrain
{
    public static class TextureGenerator
    {
        public static Texture2D TextureFromColorMap(MapData mapData, int offsetX, int offsetY, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] offsetColorMap = new Color[width * height];
            for (int y = 0; y < height; ++y)
            {
                Array.Copy(mapData.colorMap, (y + offsetY) * mapData.verticesX + offsetX, offsetColorMap, y * width, width);
            }

            texture.SetPixels(offsetColorMap);
            texture.Apply();

            return texture;
        }

        public static Texture2D TextureFromHeightMap(MapData mapData)
        {
            for (int y = 0; y < mapData.verticesY; ++y)
            {
                for (int x = 0; x < mapData.verticesX; ++x)
                {
                    mapData.colorMap[y * mapData.verticesX + x] = Color.Lerp(Color.white, Color.black, mapData.heightMap[x, y]);
                }
            }

            return TextureFromColorMap(mapData, 0, 0, MapGenerator.chunkSize, MapGenerator.chunkSize);
        }
    }
}