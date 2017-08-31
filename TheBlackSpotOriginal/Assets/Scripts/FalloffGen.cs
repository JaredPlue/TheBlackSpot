using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGen {

    /**
     *  Generates a falloff map, a map where the outside values are all 1, this will be subtracted from our map to make an island
     */
    public static float[,] GenFalloffMap(int size)
    {
        float[,] map = new float[size, size];
        for(int i = 0; i < size; i++)
        {
            for(int j=0; j< size; j++)
            {
                float x = i / (float)size * 2 - 1; //value from -1 - 1 
                float y = j / (float)size * 2 - 1;

                float closestToOne = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(closestToOne);
            }
        }

        return map;
    }

    static float Evaluate(float value)
    {
        float a = 3;
        float b = 2.2f;
        return Mathf.Pow(value,a)/(Mathf.Pow(value,a) + Mathf.Pow(b - b * value, a)); 
        //x^a / x^a + (b - bx)^a // makes a nice curve for  a transition of black -> white for falloff map
    }
}
