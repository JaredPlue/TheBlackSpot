using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public float uniformScale = 5f; //large scale produces desirable char height (x,y,z)

    public float meshHeightMultiplier; //(y)
    public AnimationCurve meshHeightCurve;
    public bool useFalloff;           

    public float minHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(0);
        }
    }

    public float maxHeight
    {
        get
        {
            return uniformScale * meshHeightMultiplier * meshHeightCurve.Evaluate(1);
        }
    }

    public float getHeight(float[,] heightMap, int x, int z)
    { 
        float height = meshHeightCurve.Evaluate(heightMap[x,z]) * meshHeightMultiplier;
        return height;
    }
}
