using UnityEngine;

public static class Noise
{
    public enum NormalizerMode
    {
        Local, // for using local minimum and maximum height
        Global // for esimating a global min and max height
    }

    public static float[,] GenerateNoiseMap(
        int mapWidth,
        int mapHeight,
        int seed,
        float noiseScale,
        int numberOfOctaves,
        float persistence,
        float lacunarity,
        Vector2 offset,
        NormalizerMode normalizer
        )
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[numberOfOctaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;

        for (int i = 0; i < numberOfOctaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistence;
        }


        if (noiseScale <= 0)
            noiseScale = 0.0001f;

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                float frequency = 1;
                float noiseHeight = 1;

                for (int i = 0; i < numberOfOctaves; i++)
                {
                    // choose sample points
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / noiseScale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / noiseScale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // guarantees negative values too
                    noiseHeight += perlinValue * amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                    maxLocalNoiseHeight = noiseHeight;
                else if (noiseHeight < minLocalNoiseHeight)
                    minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (normalizer == NormalizerMode.Local)
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                else
                {
                    float normalizedHeight = ((noiseMap[x, y] + 1) / 2f) / maxPossibleHeight;
                    normalizedHeight *= 1.07f;
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0f, float.MaxValue);
                }
            }
        }

        return noiseMap;
    }
}
