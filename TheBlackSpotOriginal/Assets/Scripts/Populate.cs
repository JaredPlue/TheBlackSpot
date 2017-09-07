using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Populate : MonoBehaviour
{
    static MapGen mapGen;
    TerrainData terrainData;

    public GameObject tree;
    private int number;
    private int xaxis;
    private int yaxis;
    private float zaxis;
    private Transform[] spawnLocations;
    private GameObject[] treePrefab;
    private GameObject[] treePrefabClone;


    void Start()
    {

        Debug.Log("place trees called");
        mapGen = FindObjectOfType<MapGen>();
        terrainData = mapGen.terrainData;

        PlaceTrees();
        Debug.Log("place trees called");
    }

    void PlaceTrees()
    {
        Vector3[] posns;
        posns = GeneratePositions(); //generate 100 posns
        shuffle(posns);

        //for (int k = 0; k < 25; k++)
        //{
        //   spawnLocations[k] = new Transform(posns[k], Quaternion.identity, 1.0);
        //}

        for (int i = 0; i < 25; i++) //take the first 25 locations, make trees
        {
            Debug.Log("spawned tree");
            Instantiate(tree, posns[i], Quaternion.identity); //as GameObject
        }
    }

    Vector3[] GeneratePositions()
    {
        Vector3[] posns = new Vector3[100];
        int x = 0;
        float y = 0;
        int z = 0;
        Vector3 tmpPosn = new Vector3(x, y, z);
        Terrain tmp1 = new Terrain();
        int i = 0;
        float[,] heightMapPosn = new float[x, z];
        int j = 0;
        //float[,] tmpheightmap = GetComponent<MapData>().GetHeightMap();

        Debug.Log("before while loop");
       //while (posns[i] != posns[posns.Length-1]) //while not full
       while(true)
        //while (j != 100)
        {
            Debug.Log("inside while loop");
            x = Random.Range(-MapGen.mapChunkSize, MapGen.mapChunkSize);  //gen rand x (-254,254)
            Debug.Log(x.ToString());
            z = Random.Range(-MapGen.mapChunkSize, MapGen.mapChunkSize);  //gen rand z
                                                                                     
            // randz = UnityEngine.Random.Range(15, 35); //gen num 15-35                                                                   
            //z = randz / 100; //now num is .15-.35                                                              
            //float height = tmpCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
            //tmpPosn.Set(x, y, z);
            // RaycastHit hit;
            // Ray ray = new Ray(tmpPosn, Vector3.down);
            // if(GetComponent<Collider>().Raycast(ray, out hit, 2.0f * maxHeight))
            // {
            //     Debug.Log("Hit Point: " + hit.point);
            //  }
            
            heightMapPosn = new float[x,z];
            Debug.Log(heightMapPosn);
            float currHeight = terrainData.getHeight(heightMapPosn, x, z);
            //tmpPosn.y = Terrain.activeTerrain.SampleHeight(tmpPosn) + Terrain.activeTerrain.GetPosition().y;
            if (currHeight < 0.35 && currHeight > 0.15)
            {
                posns[j] = new Vector3(x, (currHeight * terrainData.meshHeightCurve.Evaluate(currHeight) * terrainData.meshHeightMultiplier), z); //include it
                Debug.Log(posns[j]);
                j++;
                Debug.Log("position added");
            }
        }
        return posns; //now has 100 valid positions to spawn trees
    }

    Vector3[] shuffle(Vector3[] posns)
    {
        // Knuth shuffle algorithm :: courtesy of Wikipedia
        for (int i = 0; i < posns.Length; i++)
        {
            Vector3 tmp = posns[i];
            int r = Random.Range(i, posns.Length);
            posns[i] = posns[r];
            posns[r] = tmp;
        }
        Debug.Log("shuffled");
        return new Vector3[1];
    }

    /**
    bool canSpawn(int z)
    {
        float scale;
        scale = GetComponent<MapGen>().terrainData.uniformScale;

        if (z * scale < .15 * scale || z * scale > 0.35 * scale) //outside bounds
        {
            return false;
        }
        else return true; //within bounds
    }
    */
}













////////////////////////////////POOR ATTEMPT BELOW /////////////////////////////
/*
public class Populate : MonoBehaviour
{
    
    public GameObject tree;
    public Terrain copy;
    private int number;
    private int xaxis;
    private int yaxis;
    private float zaxis;
    private Transform[] spawnLocations;
    private GameObject[] treePrefab;
    private GameObject[] treePrefabClone;


    void Start()
    {

        Debug.Log("place trees called");
        waitTwoSeconds();
        PlaceTrees();
        Debug.Log("place trees called");
    }

    IEnumerator waitTwoSeconds()
    {
        yield return new WaitForSeconds(2);
    }

    void PlaceTrees()
    {
        Vector3[] posns;
        posns = GeneratePositions(); //generate 100 posns
        shuffle(posns);
        
        for (int k = 0; k < 25; k++)
        {
            spawnLocations[k] = new Transform(posns[k], Quaternion.identity, 1.0);
        }
        
        for (int i = 0; i < 25; i++) //take the first 25 locations, make trees
        {
            Debug.Log("spawned tree");
            Instantiate(treePrefab[i], posns[i], Quaternion.identity); //as GameObject
        }
    }

    Vector3[] GeneratePositions()
    {
        Vector3[] posns = new Vector3[100];
        int x = 0;
        int y = 0;
        int randz = 0;
        float z = 0;
        Vector3 tmpPosn = new Vector3(x,y,z);
        Terrain tmp1 = new Terrain();
        int i = 0;
        int maxHeight = 500;
        //float[,] tmpheightmap = GetComponent<MapData>().GetHeightMap();

        while (true)
        {
            x = UnityEngine.Random.Range(-MapGen.mapChunkSize, MapGen.mapChunkSize);  //gen rand x
            y = UnityEngine.Random.Range(-MapGen.mapChunkSize, MapGen.mapChunkSize);  //gen rand y
            randz = UnityEngine.Random.Range(15, 35); //gen num 15-35
            z = randz / 100; //now num is .15-.35
                             //float height = tmpCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
            tmpPosn.Set(x, y, z);
           // RaycastHit hit;
           // Ray ray = new Ray(tmpPosn, Vector3.down);
           // if(GetComponent<Collider>().Raycast(ray, out hit, 2.0f * maxHeight))
           // {
           //     Debug.Log("Hit Point: " + hit.point);
          //  }

            tmpPosn.y = Terrain.activeTerrain.SampleHeight(tmpPosn) + Terrain.activeTerrain.GetPosition().y;
            if(Terrain.activeTerrain.GetPosition().y < 0.35 && Terrain.activeTerrain.GetPosition().y > 0.15)
            {
                posns[i] = new Vector3(x, y, Terrain.activeTerrain.GetPosition().y); //include it
                i++;
                Debug.Log("position added");
            }
            /***
            if (tmpheightmap[x, y] < 0.35 && tmpheightmap[x, y] > 0.15)
            {
                posns[i] = new Vector3(x, y, GetComponent<MapData>().heightMap[x, y]); //include it
                i++;
                Debug.Log("position added");
            }
            
            if(posns[i] == posns[posns.Length - 1])
            {
                Debug.Log("array full");
                break; //if the array is full of okay posns
            }
        }
        return posns; //now has 100 valid positions to spawn trees
    }

    Vector3[] shuffle(Vector3[] posns)
    {
        // Knuth shuffle algorithm :: courtesy of Wikipedia
        for (int i = 0; i < posns.Length; i++)
        {
            Vector3 tmp = posns[i];
            int r = Random.Range(i, posns.Length);
            posns[i] = posns[r];
            posns[r] = tmp;
        }
        Debug.Log("shuffled");
        return new Vector3[1];
    }

    bool canSpawn(int z)
    {
        float scale;
        scale = GetComponent<MapGen>().terrainData.uniformScale;

        if (z * scale < .15 * scale || z * scale > 0.35 * scale) //outside bounds
        {
            return false;
        }
        else return true; //within bounds
    }
}

    */
