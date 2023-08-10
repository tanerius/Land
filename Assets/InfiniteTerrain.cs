using System;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    const float scale = 1f;
    const float viewerMoveThresholdBeforeUpdate = 25f;
    const float sqrViewerMoveThresholdBeforeUpdate = viewerMoveThresholdBeforeUpdate * viewerMoveThresholdBeforeUpdate;

    public LODInfo[] detailLevels;

    public static float maxViewDistance;



    public Transform viewerTransform;
    public Material mapMaterial;
    public static Vector2 viewerPosition;
    Vector2 previousViewerPosition;
    static MapGenerator mapGenerator;

    int chunkSize;
    int visibleChunksInViewDistance;

    Dictionary<Vector2, TerrainChunk> spawnedChunksDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        visibleChunksInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewerTransform.position.x, viewerTransform.position.z) / scale;

        if ((previousViewerPosition - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdBeforeUpdate)
        {
            previousViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        foreach (var item in terrainChunksVisibleLastUpdate)
        {
            item.SetVisible(false);
        }

        terrainChunksVisibleLastUpdate.Clear();

        int currentCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for (int yOffset = -visibleChunksInViewDistance; yOffset <= visibleChunksInViewDistance; yOffset++)
        {
            for (int xOffset = -visibleChunksInViewDistance; xOffset <= visibleChunksInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(xOffset + currentCoordX, yOffset + currentCoordY);

                if (spawnedChunksDict.ContainsKey(viewedChunkCoord))
                {
                    spawnedChunksDict[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    spawnedChunksDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds; // used to find point on perimeter of the object
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        LODInfo[] detailLevelsArray;
        LODMesh[] meshesLODArray;
        MapData mapData;
        bool mapDataReceived;
        int previousLODindex = -1;

        public TerrainChunk(Vector2 coordinate, int size, LODInfo[] d, Transform parent, Material material)
        {
            mapDataReceived = false;
            detailLevelsArray = d;
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionv3 = new Vector3(position.x, 0, position.y); // project onto 3d spce

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material;

            meshObject.transform.position = positionv3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            meshesLODArray = new LODMesh[detailLevelsArray.Length];

            for (int i = 0; i < detailLevelsArray.Length; i++)
            {
                meshesLODArray[i] = new LODMesh(detailLevelsArray[i].lod, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }

        /// <summary>
        /// Called when a map data generator thread exists returning a generated MapData object
        /// </summary>
        /// <param name="mapData"></param>
        private void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }


        public void UpdateTerrainChunk()
        {
            if (!mapDataReceived) return;

            // a method to find the point on the meshes perimeter which is closest to the viewer.
            // if this point is >= maxViewDistance then the mesh should be disabled
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            bool visible = viewerDistanceFromNearestEdge <= maxViewDistance;

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < detailLevelsArray.Length - 1; i++)
                {
                    if (viewerDistanceFromNearestEdge > detailLevelsArray[i].visibleDistanceThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                        break;
                }

                if (lodIndex != previousLODindex)
                {
                    LODMesh lodMesh = meshesLODArray[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODindex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(mapData);
                    }
                }

                terrainChunksVisibleLastUpdate.Add(this);
            }

            SetVisible(visible);


        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible => meshObject.activeSelf;
    }

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        Action updateCallback;

        public LODMesh(int lod, Action updateCallback)
        {
            this.lod = lod;
            this.hasRequestedMesh = false;
            this.hasMesh = false;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }
}
