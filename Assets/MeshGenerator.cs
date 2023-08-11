using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve meshHeightCurve, int lodStepsIncrement)
    {
        AnimationCurve threadSpecificHeightCurve = new AnimationCurve(meshHeightCurve.keys);
        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2 * lodStepsIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int veticesPerLine = (meshSize - 1) / lodStepsIncrement + 1;

        MeshData meshData = new MeshData(veticesPerLine);
        int[,] vertexIndecesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += lodStepsIncrement)
        {
            for (int x = 0; x < borderedSize; x += lodStepsIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if(isBorderVertex)
                {
                    vertexIndecesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndecesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += lodStepsIncrement)
        {
            for (int x = 0; x < borderedSize; x += lodStepsIncrement)
            {
                int vertexIndex = vertexIndecesMap[x, y];
                Vector2 percent = new Vector2((x - lodStepsIncrement) / (float)meshSize, (y - lodStepsIncrement) / (float)meshSize);
                float height = threadSpecificHeightCurve.Evaluate(heightMap[x, y]);
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height * heightMultiplier, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                // ignoring the right and bottom edhe vertices of the map as we dont need to traverse them
                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    // create two triangles out of 4 points
                    int a = vertexIndecesMap[x, y];
                    int b = vertexIndecesMap[x + lodStepsIncrement, y];
                    int c = vertexIndecesMap[x, y + lodStepsIncrement];
                    int d = vertexIndecesMap[x + lodStepsIncrement, y + lodStepsIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
                vertexIndex++;
            }
        }

        meshData.BakeNormals();

        return meshData;
    }
}

public class MeshData
{
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    private Vector3[] borderVertices;
    private int[] borderTriangles;
    private Vector3[] bakedNormals; // used to calculatye normals in a new thread instead of the main

    private int triangleIndex = 0;
    private int borderTriangleIndex;

    public MeshData(int verticesPerLine)
    {
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[verticesPerLine * 6 * 4];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            // This is a border index
            borderVertices[-vertexIndex - 1] = vertexPosition;

        }
        else
        {
            // This is a mesh index
            vertices[vertexIndex] = vertexPosition;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if(a < 0 || b < 0 || c < 0) 
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;
            borderTriangleIndex += 3;
        }
        else
        {
            triangles[triangleIndex] = a;
            triangles[triangleIndex + 1] = b;
            triangles[triangleIndex + 2] = c;
            triangleIndex += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        // An array to store the vertex normals in
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        // figure out how many triangles we have
        int triangleCount = triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndeces(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndeces(vertexIndexA, vertexIndexB, vertexIndexC);
            if(vertexIndexA >= 0)
                vertexNormals[vertexIndexA] += triangleNormal;
            if(vertexIndexB >= 0)
                vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0)
                vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0;i< vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndeces(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        // now we calculate the surface normal from these points forming a triangle surface
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        // Get the perpendicular normal for the side
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        //mesh.RecalculateNormals();
        // Use our function to calculate normals
        mesh.normals = bakedNormals;
        return mesh;
    }

    public void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }
}