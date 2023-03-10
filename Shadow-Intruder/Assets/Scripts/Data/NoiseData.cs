using UnityEngine;

[System.Serializable]
public struct TerrainType
{
    public string name;
    [Range(0, 1)]
    public float height;
    public Color color;
}

[CreateAssetMenu]
public class NoiseData : UpdateData
{
    public int octaves;
    [Range(0, 1)]
    public float persistance;
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

        base.OnValidate();
    }
}
