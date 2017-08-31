using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class MapGen : MonoBehaviour {

    public enum DrawMode{NoiseMap, Mesh, FalloffMap};
    public DrawMode drawMode;

    [Range(0, 6)]
    public int editorLOD; //1 for no simplification, 2/4/6 ect for detail 
    public bool autoUpdate;

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;
    public bool useFlatShading;

    float[,] falloffMap;
    static MapGen instance;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQ = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQ = new Queue<MapThreadInfo<MeshData>>();

    void onValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void onTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }


    public static int mapChunkSize
    {
        get{
            if(instance == null)
            {
                instance = FindObjectOfType<MapGen>();
            }
            if (instance.useFlatShading)
            {
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {

        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(mapData.heightMap));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(MeshGen.GenTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorLOD, useFlatShading)); 
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            display.DrawTexture(TextureGen.TextureFromHeightMap(FalloffGen.GenFalloffMap(mapChunkSize)));
        }
    }

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };
        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQ)
        {
            mapDataThreadInfoQ.Enqueue(new MapThreadInfo<MapData>(callback, mapData)); //restricts to one thread at a time using this code
        }
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, callback);
        };

        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, Action < MeshData > callback)
    {
         MeshData meshData = MeshGen.GenTerrainMesh(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, lod, useFlatShading);
         lock (meshDataThreadInfoQ)
         {
              meshDataThreadInfoQ.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
         }
    }

    private void Update()
    {
        if(mapDataThreadInfoQ.Count > 0)
        {
            for (int i = 0; i < mapDataThreadInfoQ.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQ.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
        if (meshDataThreadInfoQ.Count > 0)
        {
            for (int i = 0; i < meshDataThreadInfoQ.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQ.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }
    

    MapData GenerateMapData(Vector2 center){
        //chunksize +2 for border triangles of top/bot left/right
        float[,] noiseMap = Noise.GenerateNoiseMap(mapChunkSize + 2, mapChunkSize + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistance, noiseData.lacunarity, center + noiseData.offset, noiseData.normalizeMode);

        if (terrainData.useFalloff)
        {
            if(falloffMap == null)
            {
                falloffMap = FalloffGen.GenFalloffMap(mapChunkSize + 2);
            }
            for (int y = 0; y < mapChunkSize + 2; y++)
            {
                for (int x = 0; x < mapChunkSize + 2; x++)
                {
                    if (terrainData.useFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]); //if we're using the falloff, subtract it
                    }
                }
            }
        }
        textureData.UpdateMeshHeights(terrainMaterial, terrainData.minHeight, terrainData.maxHeight);
        return new MapData(noiseMap);
    }

     void OnValidate(){

        if(terrainData != null)
        {
            terrainData.OnValuesUpdated -= onValuesUpdated; //before you subscribe, unsubscribe (subscription ct = 1)
            terrainData.OnValuesUpdated += onValuesUpdated;
        }
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= onValuesUpdated;
            noiseData.OnValuesUpdated += onValuesUpdated;
        }
        if(textureData != null)
        {
            textureData.OnValuesUpdated -= onTextureValuesUpdated;
            textureData.OnValuesUpdated += onTextureValuesUpdated;
        }
    }

    struct MapThreadInfo<T> //generic for mesh and map data
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo (Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}


public struct MapData
{
    public readonly float[,] heightMap; //readonly is basically const for a struct param

    public MapData(float[,] heightMap)
    {
        this.heightMap = heightMap;
    }
}