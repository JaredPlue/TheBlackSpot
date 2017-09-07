using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour {


    public LODInfo[] detailLevels;
    public static float maxViewDist = 600; //matches last value in detail level arr

    public Transform viewer; //so we can see their posn
    public Material mapMaterial;
    public static Vector2 viewerPosn;
    public static Vector2 viewerPosnOld;
    static MapGen mapGen;
    int chunkSize;
    int chunkCount;
    const float viewerMinDistForUpdate = 25f;
    const float sqrViewerMinDistForUpdate = viewerMinDistForUpdate * viewerMinDistForUpdate;
    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksFromLastUpdate = new List<TerrainChunk>();

    void Start()
    {
        mapGen = FindObjectOfType<MapGen>();
        maxViewDist = detailLevels[detailLevels.Length - 1].visibleDist;
        chunkSize = MapGen.mapChunkSize - 1;
        chunkCount = Mathf.RoundToInt(maxViewDist / chunkSize);
        UpdateVisibleChunks(); //update once at start
    }

    void Update() //updates viewerposns once per frame
    {
        viewerPosn = new Vector2(viewer.position.x, viewer.position.z) / mapGen.terrainData.uniformScale;
        if ((viewerPosnOld - viewerPosn).sqrMagnitude > sqrViewerMinDistForUpdate)
        {
            viewerPosnOld = viewerPosn;
            UpdateVisibleChunks(); //only update if viewer has moved enough
        }
    }


    void UpdateVisibleChunks()
    {

        for(int i = 0; i < terrainChunksFromLastUpdate.Count; i++)
        {
            terrainChunksFromLastUpdate[i].SetVisible(false); //each frame, flush the chunks
        }

        terrainChunksFromLastUpdate.Clear(); //clear old list of chunks

        int currentChunkposnX = Mathf.RoundToInt(viewerPosn.x / chunkSize);
        int currentChunkposnY = Mathf.RoundToInt(viewerPosn.y / chunkSize);

        for(int yOffset = -chunkCount; yOffset <= chunkCount; yOffset++)
        {
            for (int xOffset = -chunkCount; xOffset <= chunkCount; xOffset++)
            {
                Vector2 viewedChunkPosn = new Vector2(currentChunkposnX + xOffset, currentChunkposnY + yOffset);

                if (terrainChunkDict.ContainsKey(viewedChunkPosn))
                {
                    terrainChunkDict[viewedChunkPosn].UpdateChunk(); //updates terrain chunk
                }
                else
                {
                    terrainChunkDict.Add(viewedChunkPosn, new TerrainChunk(viewedChunkPosn, chunkSize, detailLevels, transform, mapMaterial));
                }

            }
        }
    }

    public class TerrainChunk
    {
        Vector2 posn;
        GameObject meshObject;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODmesh[] lodMeshes;
        LODmesh collisionLODMesh;
        MapData mapData;
        bool mapDataReceived;
        int previousLODidx = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            posn = coord * size;
            Vector3 posnV3 = new Vector3(posn.x, 0, posn.y);
            bounds = new Bounds(posn, Vector2.one * size);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = posnV3 * mapGen.terrainData.uniformScale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * mapGen.terrainData.uniformScale;
            SetVisible(false); //set to false

            lodMeshes = new LODmesh[detailLevels.Length];
            for(int i = 0; i<detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODmesh(detailLevels[i].lod, UpdateChunk); //make an array containing level of details for each surrounding chunk
                if (detailLevels[i].useForCollider)
                {
                    collisionLODMesh = lodMeshes[i];
                }
            }

            mapGen.RequestMapData(posn, OnMapDataReceived);
        }

        
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            UpdateChunk();
        }
        
        public void UpdateChunk()
        {

            if (mapDataReceived)
            {
                float viewerDistFromClosestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosn));
                bool makeVisible = viewerDistFromClosestEdge <= maxViewDist;
                SetVisible(makeVisible); //dflt false

                if (makeVisible) //if its visible
                {
                    int lodIdx = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++) //loop through
                    {
                        if (viewerDistFromClosestEdge > detailLevels[i].visibleDist)
                        {
                            lodIdx = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIdx != previousLODidx)
                    {
                        LODmesh lodMesh = lodMeshes[lodIdx];
                        if (lodMesh.meshReceived)
                        {
                            previousLODidx = lodIdx;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.meshRequested)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    if(lodIdx == 0)
                    {
                        //only at highest res add collisions
                        if (collisionLODMesh.meshReceived)
                        {
                            meshCollider.sharedMesh = collisionLODMesh.mesh;
                        }
                        else if (!collisionLODMesh.meshRequested)
                        {
                            collisionLODMesh.RequestMesh(mapData);
                        }
                    }
                    terrainChunksFromLastUpdate.Add(this);
                }
                SetVisible(makeVisible);
            }
        }

        public void SetVisible(bool makeVisible)
        {
            meshObject.SetActive(makeVisible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    class LODmesh //each terrain gets a level of detail
    {
        public Mesh mesh;
        public bool meshRequested;
        public bool meshReceived;
        int lod;
        System.Action updateCallback;

        public LODmesh(int lod, System.Action updateCallback)
        {
            this.updateCallback = updateCallback;
            this.lod = lod;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            meshReceived = true;
            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            meshRequested = true;
            mapGen.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDist; //if viewer is not as close, we want lower lod
        public bool useForCollider;
    }

}
