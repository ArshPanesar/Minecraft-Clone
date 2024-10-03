using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMeshGeneratorTask : TaskManager.Task
{
    public GameObject inout_ChunkGameObject;
    public int[,] in_HeightMap;

    float BlockMeshSize = 1.0f;

    public override void Execute() 
    {
        Mesh ChunkMesh = Generate();

        inout_ChunkGameObject.GetComponent<MeshFilter>().mesh = ChunkMesh;
        inout_ChunkGameObject.GetComponent<MeshRenderer>().material = WorldData.TextureAtlasMaterial;
    }

    public Mesh Generate()
    {
        Mesh ChunkMesh = new Mesh();
        
        // Create the Mesh
        //
        // Surface Quads
        int quad_vertexCount = 4 * WorldData.ChunkSize * WorldData.ChunkSize;
        int quad_triangleCount = 3 * 2 * WorldData.ChunkSize * WorldData.ChunkSize;
        Vector3[] quad_vertices = new Vector3[quad_vertexCount];
        Vector2[] quad_uv = new Vector2[quad_vertexCount];
        int[] quad_triangles = new int[quad_triangleCount];
        
        Vector3 PositionOffset = new Vector3(0.0f, 0.0f, 0.0f);
        int hx = 0, hz = 0;
        for (int i = 0; i < quad_vertexCount; i += 4)
        {
            int BlockHeight = in_HeightMap[hx, hz];

            quad_vertices[i + 0] = new Vector3(0.0f, BlockHeight, 0.0f) + PositionOffset;
            quad_vertices[i + 1] = new Vector3(BlockMeshSize, BlockHeight, 0.0f) + PositionOffset;
            quad_vertices[i + 2] = new Vector3(0.0f, BlockHeight, BlockMeshSize) + PositionOffset;
            quad_vertices[i + 3] = new Vector3(BlockMeshSize, BlockHeight, BlockMeshSize) + PositionOffset;

            quad_uv[i + 0] = new Vector2(0.0f, 0.0f);
            quad_uv[i + 1] = new Vector2(1.0f, 0.0f);
            quad_uv[i + 2] = new Vector2(0.0f, 30.0f / 96.0f);
            quad_uv[i + 3] = new Vector2(1.0f, 30.0f / 96.0f);

            PositionOffset.x += BlockMeshSize;
            ++hx;
            if ((int)PositionOffset.x >= WorldData.ChunkSize)
            {
                PositionOffset.x = 0.0f;
                PositionOffset.z += BlockMeshSize;

                hx = 0;
                ++hz;
            }
        }

        int vi = 0;
        for (int i = 0; i < quad_triangleCount; i += 6)
        {
            quad_triangles[i + 0] = vi + 0;
            quad_triangles[i + 1] = vi + 1;
            quad_triangles[i + 2] = vi + 2;

            quad_triangles[i + 3] = vi + 1;
            quad_triangles[i + 4] = vi + 2;
            quad_triangles[i + 5] = vi + 3;

            vi += 4;
        }

        // Sides Vertices
        List<Vector3> side_vertices = new List<Vector3>(4 * WorldData.ChunkSize * WorldData.ChunkSize);

        hx = 0;
        hz = 0;
        for (int i = 0; i < quad_vertexCount; i += 4)
        {
            int BlockHeight = in_HeightMap[hx, hz];
            int NextBlockHeight = in_HeightMap[hx, hz + 1];

            int SideHeight = NextBlockHeight - BlockHeight;
            if (SideHeight != 0)
            {
                side_vertices.Add(quad_vertices[i + 2]);
                side_vertices.Add(quad_vertices[i + 3]);
                side_vertices.Add(quad_vertices[i + 2] + new Vector3(0.0f, SideHeight, 0.0f));
                side_vertices.Add(quad_vertices[i + 3] + new Vector3(0.0f, SideHeight, 0.0f));
            }

            ++hz;
            if (hz >= WorldData.ChunkSize - 1)
            {
                hz = 0;
                ++hx;
                i += 4; // Skip the Last Quad
            }
        }

        int[] side_triangles = new int[3 * (side_vertices.Count / 2)];
        vi = quad_vertexCount;
        for (int i = 0; i < side_triangles.Length; i += 6)
        {
            side_triangles[i + 0] = vi + 0;
            side_triangles[i + 1] = vi + 1;
            side_triangles[i + 2] = vi + 2;

            side_triangles[i + 3] = vi + 1;
            side_triangles[i + 4] = vi + 2;
            side_triangles[i + 5] = vi + 3;

            vi += 4;
        }

        int totalVertexCount = quad_vertexCount + side_vertices.Count;
        int totalTriangleCount = quad_triangleCount + side_triangles.Length;
        Vector3[] total_vertices = new Vector3[totalVertexCount];
        int[] total_triangles = new int[totalTriangleCount];
        int ti = 0;
        for (int i = 0; i < quad_vertexCount; ++i)
        {
            total_vertices[ti] = quad_vertices[i];
            ++ti;
        }
        for (int i = 0; i < side_vertices.Count; ++i)
        {
            total_vertices[ti] = side_vertices[i];
            ++ti;
        }
        ti = 0;
        for (int i = 0; i < quad_triangleCount; ++i)
        {
            total_triangles[ti] = quad_triangles[i];
            ++ti;
        }
        for (int i = 0; i < side_triangles.Length; ++i)
        {
            total_triangles[ti] = side_triangles[i];
            ++ti;
        }

        Debug.Log(quad_vertices.Length);
        Debug.Log(total_vertices.Length);
        // Assign the Mesh Attributes
        ChunkMesh.vertices = total_vertices;
        ChunkMesh.triangles = total_triangles;
        //ChunkMesh.uv = quad_uv;

        return ChunkMesh;
    }
}

//vertices[0] = new Vector3(0.0f, 0.0f, 0.0f);
//vertices[1] = new Vector3(1.0f, 0.0f, 0.0f);
//vertices[2] = new Vector3(0.0f, 1.0f, 0.0f);
//vertices[3] = new Vector3(1.0f, 1.0f, 0.0f);

//vertices[4] = new Vector3(0.0f, 1.0f, 1.0f);
//vertices[5] = new Vector3(1.0f, 1.0f, 1.0f);
//vertices[6] = new Vector3(0.0f, 0.0f, 1.0f);
//vertices[7] = new Vector3(1.0f, 0.0f, 1.0f);


//triangles[0] = 0;
//triangles[1] = 2;
//triangles[2] = 1;

//triangles[3] = 1;
//triangles[4] = 2;
//triangles[5] = 3;

//triangles[6] = 2;
//triangles[7] = 4;
//triangles[8] = 3;

//triangles[9] = 4;
//triangles[10] = 5;
//triangles[11] = 3;

//triangles[12] = 6;
//triangles[13] = 4;
//triangles[14] = 7;

//triangles[15] = 7;
//triangles[16] = 4;
//triangles[17] = 5;

//triangles[18] = 0;
//triangles[19] = 6;
//triangles[20] = 1;

//triangles[21] = 1;
//triangles[22] = 6;
//triangles[23] = 7;

//triangles[24] = 3;
//triangles[25] = 7;
//triangles[26] = 1;

//triangles[27] = 3;
//triangles[28] = 5;
//triangles[29] = 7;

//triangles[30] = 0;
//triangles[31] = 6;
//triangles[32] = 2;

//triangles[33] = 6;
//triangles[34] = 2;
//triangles[35] = 4;

//uv[0] = new Vector2(0, 0);
//uv[1] = new Vector2(32.0f / 32.0f, 0);
//uv[2] = new Vector2(32.0f / 32.0f, 32.0f / 96.0f);
//uv[3] = new Vector2(0, 32.0f / 96.0f);

//uv[4] = new Vector2(0, 0);
//uv[5] = new Vector2(32.0f / 32.0f, 0);
//uv[6] = new Vector2(32.0f / 32.0f, 32.0f / 96.0f);
//uv[7] = new Vector2(0, 32.0f / 96.0f);