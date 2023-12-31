using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData mesh, Texture2D texture)
    {
        // should be sharedmesh cuz we are generating the mesh outside the game mode
        meshFilter.sharedMesh = mesh.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
