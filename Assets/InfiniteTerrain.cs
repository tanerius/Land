using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrain : MonoBehaviour
{
    public const float maxViewDistance = 450f;
    public Transform viewerTransform;

    public static Vector2 viewerPosition;
    int chunkSize;
    int visibleChunksInViewDistance;

    Dictionary<Vector2,TerrainChunk> spawnedChunksDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        chunkSize = MapGenerator.mapChunkSize - 1;
        visibleChunksInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewerTransform.position.x, viewerTransform.position.z);
        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        foreach(var item in terrainChunksVisibleLastUpdate)
        {
            item.SetVisible(false);
        }

        terrainChunksVisibleLastUpdate.Clear();

        int currentCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        for(int yOffset = -visibleChunksInViewDistance; yOffset <= visibleChunksInViewDistance; yOffset++)
        {
            for (int xOffset = -visibleChunksInViewDistance; xOffset <= visibleChunksInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(xOffset + currentCoordX, yOffset + currentCoordY);

                if(spawnedChunksDict.ContainsKey(viewedChunkCoord)) 
                {
                    spawnedChunksDict[viewedChunkCoord].UpdateTerrainChunk();
                    if (spawnedChunksDict[viewedChunkCoord].IsVisible)
                    {
                        terrainChunksVisibleLastUpdate.Add(spawnedChunksDict[viewedChunkCoord]);
                    }
                }
                else
                {
                    spawnedChunksDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform));
                }
            }
        }
    }

    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 position;
        Bounds bounds; // used to find point on perimeter of the object
        public TerrainChunk(Vector2 coordinate, int size, Transform parent = null)
        {
            position = coordinate * size;
            bounds = new Bounds(position, Vector2.one * size);
            Vector3 positionv3 = new Vector3(position.x, 0, position.y); // project onto 3d spce

            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            meshObject.transform.position = positionv3;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            meshObject.transform.parent = parent;
            SetVisible(false);
        }

        public void UpdateTerrainChunk()
        {
            // a method to find the point on the meshes perimeter which is closest to the viewer.
            // if this point is >= maxViewDistance then the mesh should be disabled
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
            SetVisible(viewerDistanceFromNearestEdge <= maxViewDistance);
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible => meshObject.activeSelf;
    }
}
