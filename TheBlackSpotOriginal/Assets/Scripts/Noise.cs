using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise{

    public enum NormalizeMode { Local, Global };

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octave, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode){
        float[,] noiseMap = new float[mapWidth, mapHeight];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octave];
        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octave; i++){
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0){
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2; 

        for (int y = 0; y < mapHeight; y++){
            for (int x = 0; x < mapWidth; x++){
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octave; i++){
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;  //higher freq - height values change more
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) *2 -1; //from [-1,1]
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity; //freq increases with each octave
                }

                if(noiseHeight > maxNoiseHeight){
                    maxNoiseHeight = noiseHeight; //set to max
                }
                else if(noiseHeight < minNoiseHeight){
                    minNoiseHeight = noiseHeight; //set to min
                }
                noiseMap[x, y] = noiseHeight; 
            }
        }

        //now they are in [0,1] range
        for (int y = 0; y < mapHeight; y++){
            for (int x = 0; x < mapWidth; x++){
                if (normalizeMode == NormalizeMode.Local)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / 2.5f);
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                }
            }
        }
                return noiseMap;
    }
}