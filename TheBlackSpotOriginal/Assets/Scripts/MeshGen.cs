using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGen {

    public static MeshData GenTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurve, int levelOfDetail, bool useFlatShading) {
        AnimationCurve tmpCurve = new AnimationCurve(heightCurve.keys); //get keys of the heightcurve being used

        //increment number of triangles
        int meshIncrement = (levelOfDetail == 0)?1 : levelOfDetail * 2; //if 0 set to 1, else *2

        //border + mesh size to fix seams 
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * meshIncrement;
        int meshSizeUnsimplified = borderedSize - 2;
        
        //negatives are border, and wont be included
        /* * * * * * * * * *
         * -1  -2  -3 -4  -5  *
         * -6   0   1  2  -7  *
         * -8   3   4  5  -9  *
         *-10   6   7  8  -11 *
         *-12 -13 -14 -15 -16 *
         * * * * * * * * * */

        float topLeftX = (meshSizeUnsimplified - 1) / -2f; //dont round
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f; //dont round
        
        int verticesPerLine = (meshSize - 1) / meshIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, useFlatShading);
        int[,] vertIdxMap = new int[borderedSize, borderedSize];
        int meshVertIdx = 0;
        int borderVertIdx = -1;

        for (int y = 0; y < borderedSize; y += meshIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshIncrement)
            {
                bool isBorderVert = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBorderVert)
                {
                    vertIdxMap[x, y] = borderVertIdx;
                    borderVertIdx--;
                }
                else
                {
                    vertIdxMap[x, y] = meshVertIdx;
                    meshVertIdx++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y+= meshIncrement)
        {
            for (int x = 0; x < borderedSize; x+= meshIncrement)
            {
                int vertIdx = vertIdxMap[x, y];
                Vector2 percent = new Vector2((x - meshIncrement) / (float)meshSize, (y - meshIncrement) / (float)meshSize);
                float height = tmpCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertPosn = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height , topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertPosn, percent, vertIdx);

                //creates two triangles from a,b,c,d -> TRI(ADC) and TRI(DAB)
                //a = (x,y) b = (x + i, y) c = (x, y + i) d = (x + i, y + i) where i is the meshIncrement
                if (x < borderedSize - 1 && y < borderedSize - 1) {
                    int a = vertIdxMap[x, y];
                    int b = vertIdxMap[x + meshIncrement, y];
                    int c = vertIdxMap[x, y + meshIncrement];
                    int d = vertIdxMap[x + meshIncrement, y + meshIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
                vertIdx++;
            }
        }
        meshData.ProcessMesh();
        return meshData;
    }

    public class Populate : MonoBehaviour
    {
        public GameObject tree;
        public MeshData meshData;
        public float[,] heightMap;
        private int number;
        private int xaxis;
        private int yaxis;
        private float zaxis;
        private Transform[] spawnLocations;
        private GameObject[] treePrefab;
        private GameObject[] treePrefabClone;

        //get height stuffs
        




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
            posns = GeneratePositions(meshData); //generate 100 posns
            shuffle(posns);
            /***
            for (int k = 0; k < 25; k++)
            {
                spawnLocations[k] = new Transform(posns[k], Quaternion.identity, 1.0);
            }
            */
            for (int i = 0; i < 25; i++) //take the first 25 locations, make trees
            {
                Debug.Log("spawned tree");
                Instantiate(treePrefab[i], posns[i], Quaternion.identity); //as GameObject
            }
        }

        Vector3[] GeneratePositions(MeshData meshData)
        {
            Vector3[] posns = new Vector3[100];
            int x = 0;
            int y = 0;
            int randz = 0;
            float z = 0;
            Vector3 tmpPosn = new Vector3(x, y, z);
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
                RaycastHit hit;
                Ray ray = new Ray(tmpPosn, Vector3.down);
                if (GetComponent<Collider>().Raycast(ray, out hit, 2.0f * maxHeight))
                {
                    Debug.Log("Hit Point: " + hit.point);
                }

                tmpPosn.y = Terrain.activeTerrain.SampleHeight(tmpPosn) + Terrain.activeTerrain.GetPosition().y;
                if (Terrain.activeTerrain.GetPosition().y < 0.35 && Terrain.activeTerrain.GetPosition().y > 0.15)
                {
                    posns[i] = new Vector3(x, y, Terrain.activeTerrain.GetPosition().y); //include it
                    i++;
                    Debug.Log("position added");
                }
                if (posns[i] == posns[posns.Length - 1])
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
                int r = UnityEngine.Random.Range(i, posns.Length);
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
}

    public class MeshData
    {
    Vector3[] verts;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVerts;
    int[] borderTriangles;

    int triangleIdx;
    int borderTriangleIdx;
    bool useFlatShading;

        public MeshData(int vertPerLine, bool useFlatShading)
        {
            this.useFlatShading = useFlatShading;
            verts = new Vector3[vertPerLine * vertPerLine];
            uvs = new Vector2[vertPerLine * vertPerLine];
            triangles = new int[(vertPerLine - 1) * (vertPerLine - 1) * 6];

            //number of vert in mesh * 4 for each side, + 4 for each corner
            borderVerts = new Vector3[vertPerLine * 4 + 4];
            borderTriangles = new int[24 * vertPerLine];
        }

        public void AddVertex(Vector3 vertPosn, Vector2 uv, int vertIdx)
        {
            if(vertIdx < 0)
            {
                borderVerts[-vertIdx - 1] = vertPosn;
            }
            else
            {
                verts[vertIdx] = vertPosn;
                uvs[vertIdx] = uv;
            }
        }

        public void AddTriangle(int a, int b, int c)
        {
            if (a < 0 || b < 0 || c < 0)
            {
                borderTriangles[borderTriangleIdx] = a;
                borderTriangles[borderTriangleIdx + 1] = b;
                borderTriangles[borderTriangleIdx + 2] = c;
                borderTriangleIdx += 3;
            }
            else
            {
                triangles[triangleIdx] = a;
                triangles[triangleIdx + 1] = b;
                triangles[triangleIdx + 2] = c;
                triangleIdx += 3;
            }
        }

        Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[verts.Length];
        int triangleCount = triangles.Length / 3; //number of vertices /3 = number of triangles

        for (int i = 0; i < triangleCount; i++) //for each triangle
        {
            int normalTriangleIdx = i * 3; //gives idx in triangle array
            int vertIdxA = triangles[normalTriangleIdx];
            int vertIdxB = triangles[normalTriangleIdx + 1];
            int vertIdxC = triangles[normalTriangleIdx + 2];

            Vector3 triangleNormal = TriangleNormalFromIdx(vertIdxA, vertIdxB, vertIdxC);
            vertexNormals[vertIdxA] += triangleNormal;
            vertexNormals[vertIdxB] += triangleNormal;
            vertexNormals[vertIdxC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3; //number of vertices /3 = number of triangles
        for (int i = 0; i < borderTriangleCount; i++) //for each triangle
        {
            int normalTriangleIdx = i * 3; //gives idx in triangle array
            int vertIdxA = borderTriangles[normalTriangleIdx];
            int vertIdxB = borderTriangles[normalTriangleIdx + 1];
            int vertIdxC = borderTriangles[normalTriangleIdx + 2];

            Vector3 triangleNormal = TriangleNormalFromIdx(vertIdxA, vertIdxB, vertIdxC);
            if (vertIdxA >= 0)
            {
                vertexNormals[vertIdxA] += triangleNormal;
            }
            if (vertIdxB >= 0)
            {
                vertexNormals[vertIdxB] += triangleNormal;
            }
            if (vertIdxC >= 0)
            {
                vertexNormals[vertIdxC] += triangleNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 TriangleNormalFromIdx(int idxA, int idxB, int idxC)
    {
        Vector3 ptA = (idxA < 0)?borderVerts[-idxA - 1] : verts[idxA];
        Vector3 ptB = (idxB < 0)?borderVerts[-idxB - 1] : verts[idxB];
        Vector3 ptC = (idxC < 0)?borderVerts[-idxC - 1] : verts[idxC];

        //cross product of these two vectors for the perpendicular vector

        Vector3 sideAB = ptB - ptA;
        Vector3 sideAC = ptC - ptA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh()
    {
        if (useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals(); //less expensive alternative
        }
    }

    void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[triangles.Length];
        for(int i = 0; i < triangles.Length; i++)
        {
            flatShadedUvs[i] = verts[triangles[i]];
            flatShadedVertices[i] = verts[triangles[i]];
            triangles[i] = i;
        }
        verts = flatShadedVertices;
        uvs = flatShadedUvs;
    }

        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = verts;
            mesh.triangles = triangles;
            mesh.uv = uvs;
        if (useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }
            return mesh;
        }
    }



