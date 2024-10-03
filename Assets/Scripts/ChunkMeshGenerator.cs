using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMeshGeneratorTask : TaskManager.Task
{
    public GameObject inout_ChunkGameObject;
    public int[,] in_HeightMap;
    public Chunk in_ChunkRef;
    public Vector2Int in_Position;

    float BlockMeshSize = 1.0f;
    Rect GrassTextureRect = new Rect(0.0f, 0.0f, 1.0f, 30.0f / 96.0f);
    Rect SideGrassTextureRect = new Rect(0.0f, 34.0f / 96.0f, 1.0f, 62.0f / 96.0f);
    Rect DirtTextureRect = new Rect(0.0f, 66.0f / 96.0f, 1.0f, 1.0f);

    public override void Execute() 
    {
        Mesh ChunkMesh = Generate();

        inout_ChunkGameObject.GetComponent<MeshFilter>().mesh = ChunkMesh;
        inout_ChunkGameObject.GetComponent<MeshRenderer>().material = WorldData.TextureAtlasMaterial;
        inout_ChunkGameObject.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    public Mesh Generate()
    {
        Mesh ChunkMesh = new Mesh();
        
        // Create the Mesh
        //
        // Surface Quads
        int QuadVertexCount = 4 * WorldData.ChunkSize * WorldData.ChunkSize;
        int QuadTriangleCount = 3 * 2 * WorldData.ChunkSize * WorldData.ChunkSize;
        Vector3[] QuadVertices = new Vector3[QuadVertexCount];
        Vector2[] QuadUVs = new Vector2[QuadVertexCount];
        Vector3[] QuadNormals = new Vector3[QuadVertexCount];
        int[] QuadTriangles = new int[QuadTriangleCount];

        // Prepare Light Normals (Viewed from Top)
        Vector3 QuadBottomLeftNormal = (new Vector3(0.0f, 1.0f, 0.0f)).normalized;
        Vector3 QuadBottomRightNormal = (new Vector3(0.0f, 1.0f, 0.0f)).normalized;
        Vector3 QuadTopLeftNormal = (new Vector3(0.0f, 1.0f, 0.0f)).normalized;
        Vector3 QuadTopRightNormal = (new Vector3(0.0f, 1.0f, 0.0f)).normalized;

        Vector3 PositionOffset = new Vector3(0.0f, 0.0f, 0.0f);
        int hx = 0, hz = 0;
        for (int i = 0; i < QuadVertexCount; i += 4)
        {
            int BlockHeight = in_HeightMap[hx, hz];

            QuadVertices[i + 0] = new Vector3(0.0f, BlockHeight, 0.0f) + PositionOffset;
            QuadVertices[i + 1] = new Vector3(BlockMeshSize, BlockHeight, 0.0f) + PositionOffset;
            QuadVertices[i + 2] = new Vector3(0.0f, BlockHeight, BlockMeshSize) + PositionOffset;
            QuadVertices[i + 3] = new Vector3(BlockMeshSize, BlockHeight, BlockMeshSize) + PositionOffset;

            QuadUVs[i + 0] = new Vector2(GrassTextureRect.x, GrassTextureRect.y);
            QuadUVs[i + 1] = new Vector2(GrassTextureRect.width, GrassTextureRect.y);
            QuadUVs[i + 2] = new Vector2(GrassTextureRect.x, GrassTextureRect.height);
            QuadUVs[i + 3] = new Vector2(GrassTextureRect.width, GrassTextureRect.height);

            QuadNormals[i + 0] = QuadBottomLeftNormal;
            QuadNormals[i + 1] = QuadBottomRightNormal;
            QuadNormals[i + 2] = QuadTopLeftNormal;
            QuadNormals[i + 3] = QuadTopRightNormal;

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
        for (int i = 0; i < QuadTriangleCount; i += 6)
        {
            QuadTriangles[i + 0] = vi + 0;
            QuadTriangles[i + 1] = vi + 1;
            QuadTriangles[i + 2] = vi + 2;

            QuadTriangles[i + 3] = vi + 1;
            QuadTriangles[i + 4] = vi + 2;
            QuadTriangles[i + 5] = vi + 3;

            vi += 4;
        }

        // Sides Vertices
        List<Vector3> SideVertices = new List<Vector3>(4 * WorldData.ChunkSize * WorldData.ChunkSize);
        List<Vector2> SideUVs = new List<Vector2>(SideVertices.Count);
        List<Vector3> SideNormals = new List<Vector3>(SideVertices.Count);
        
        hx = 0;
        hz = 0;
        for (int i = 0; i < QuadVertexCount; i += 4)
        {
            int BlockHeight = in_ChunkRef.GetBlockHeight(in_Position + new Vector2Int(hx, hz), true);
            int LeftBlockHeight = in_ChunkRef.GetBlockHeight(in_Position + new Vector2Int(hx - 1, hz), true);
            int RightBlockHeight = in_ChunkRef.GetBlockHeight(in_Position + new Vector2Int(hx + 1, hz), true);
            int UpBlockHeight = in_ChunkRef.GetBlockHeight(in_Position + new Vector2Int(hx, hz + 1), true);
            int DownBlockHeight = in_ChunkRef.GetBlockHeight(in_Position + new Vector2Int(hx, hz - 1), true);

            // Odd Columns don't need to rebuild Up and Down Sides
            if (hx % 2 > 0)
            {
                LeftBlockHeight = BlockHeight;
                RightBlockHeight = BlockHeight;
            }
            // Odd Rows don't need to rebuild Up and Down Sides
            if (hz % 2 > 0)
            {
                UpBlockHeight = BlockHeight;
                DownBlockHeight = BlockHeight;
            }

            if (LeftBlockHeight - BlockHeight != 0)
            {
                // First, Add the Side Grass Quad
                bool IsCurrentBlockShorter = (LeftBlockHeight - BlockHeight) > 0;
                float SideGrassHeightOffset = IsCurrentBlockShorter ? LeftBlockHeight - BlockHeight : 0.0f;
                SideVertices.Add(QuadVertices[i + 0] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(QuadVertices[i + 2] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));

                Vector2[] UVSequence = new Vector2[] 
                {
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.y),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.y)
                };
                
                SideUVs.Add(UVSequence[0]);
                SideUVs.Add(UVSequence[1]);
                SideUVs.Add(UVSequence[2]);
                SideUVs.Add(UVSequence[3]);

                float LightDirection = IsCurrentBlockShorter ? 1.0f : -1.0f;
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));

                // Then, Add the Dirt Block Side if needed
                if (Mathf.Abs(LeftBlockHeight - BlockHeight) > 1 )
                {
                    Vector3[] TempVertices = { SideVertices[SideVertices.Count - 2], SideVertices[SideVertices.Count - 1] };
                    int ResidualHeight = IsCurrentBlockShorter ? -(LeftBlockHeight - BlockHeight - 1) : -(BlockHeight - LeftBlockHeight - 1);
                    SideVertices.Add(TempVertices[0]);
                    SideVertices.Add(TempVertices[1]);
                    SideVertices.Add(TempVertices[0] + new Vector3(0.0f, ResidualHeight, 0.0f));
                    SideVertices.Add(TempVertices[1] + new Vector3(0.0f, ResidualHeight, 0.0f));

                    UVSequence = new Vector2[]
                    {
                    new Vector2(DirtTextureRect.x, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.x, DirtTextureRect.y),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.y)
                    };

                    SideUVs.Add(UVSequence[0]);
                    SideUVs.Add(UVSequence[1]);
                    SideUVs.Add(UVSequence[2]);
                    SideUVs.Add(UVSequence[3]);

                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                }
            }
            if (RightBlockHeight - BlockHeight != 0)
            {
                // First, Add the Side Grass Quad
                bool IsCurrentBlockShorter = (RightBlockHeight - BlockHeight) > 0;
                float SideGrassHeightOffset = IsCurrentBlockShorter ? RightBlockHeight - BlockHeight : 0.0f;
                SideVertices.Add(QuadVertices[i + 3] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(QuadVertices[i + 1] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));

                Vector2[] UVSequence = new Vector2[]
                {
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.y),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.y)
                };

                SideUVs.Add(UVSequence[0]);
                SideUVs.Add(UVSequence[1]);
                SideUVs.Add(UVSequence[2]);
                SideUVs.Add(UVSequence[3]);

                float LightDirection = IsCurrentBlockShorter ? -1.0f : 1.0f;
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));

                // Then, Add the Dirt Block Side if needed
                if (Mathf.Abs(RightBlockHeight - BlockHeight) > 1)
                {
                    Vector3[] TempVertices = { SideVertices[SideVertices.Count - 2], SideVertices[SideVertices.Count - 1] };
                    int ResidualHeight = IsCurrentBlockShorter ? -(RightBlockHeight - BlockHeight - 1) : -(BlockHeight - RightBlockHeight - 1);
                    SideVertices.Add(TempVertices[0]);
                    SideVertices.Add(TempVertices[1]);
                    SideVertices.Add(TempVertices[0] + new Vector3(0.0f, ResidualHeight, 0.0f));
                    SideVertices.Add(TempVertices[1] + new Vector3(0.0f, ResidualHeight, 0.0f));

                    UVSequence = new Vector2[]
                    {
                    new Vector2(DirtTextureRect.x, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.x, DirtTextureRect.y),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.y)
                    };

                    SideUVs.Add(UVSequence[0]);
                    SideUVs.Add(UVSequence[1]);
                    SideUVs.Add(UVSequence[2]);
                    SideUVs.Add(UVSequence[3]);

                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                    SideNormals.Add(new Vector3(LightDirection, 0.0f, 0.0f));
                }
            }
            if (UpBlockHeight - BlockHeight != 0)
            { 
                // First, Add the Side Grass Quad
                bool IsCurrentBlockShorter = (UpBlockHeight - BlockHeight) > 0;
                float SideGrassHeightOffset = IsCurrentBlockShorter ? UpBlockHeight - BlockHeight : 0.0f;
                SideVertices.Add(QuadVertices[i + 2] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(QuadVertices[i + 3] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));

                Vector2[] UVSequence = new Vector2[]
                {
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.y),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.y)
                };

                SideUVs.Add(UVSequence[0]);
                SideUVs.Add(UVSequence[1]);
                SideUVs.Add(UVSequence[2]);
                SideUVs.Add(UVSequence[3]);

                float LightDirection = IsCurrentBlockShorter ? -1.0f : 1.0f;
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));

                // Then, Add the Dirt Block Side if needed
                if (Mathf.Abs(UpBlockHeight - BlockHeight) > 1)
                {
                    Vector3[] TempVertices = { SideVertices[SideVertices.Count - 2], SideVertices[SideVertices.Count - 1] };
                    int ResidualHeight = IsCurrentBlockShorter ? -(UpBlockHeight - BlockHeight - 1) : -(BlockHeight - UpBlockHeight - 1);
                    SideVertices.Add(TempVertices[0]);
                    SideVertices.Add(TempVertices[1]);
                    SideVertices.Add(TempVertices[0] + new Vector3(0.0f, ResidualHeight, 0.0f));
                    SideVertices.Add(TempVertices[1] + new Vector3(0.0f, ResidualHeight, 0.0f));

                    UVSequence = new Vector2[]
                    {
                    new Vector2(DirtTextureRect.x, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.x, DirtTextureRect.y),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.y)
                    };

                    SideUVs.Add(UVSequence[0]);
                    SideUVs.Add(UVSequence[1]);
                    SideUVs.Add(UVSequence[2]);
                    SideUVs.Add(UVSequence[3]);

                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                }
            }
            if (DownBlockHeight - BlockHeight != 0)
            {
                // First, Add the Side Grass Quad
                bool IsCurrentBlockShorter = (DownBlockHeight - BlockHeight) > 0;
                float SideGrassHeightOffset = IsCurrentBlockShorter ? DownBlockHeight - BlockHeight : 0.0f;
                SideVertices.Add(QuadVertices[i + 0] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(QuadVertices[i + 1] + new Vector3(0.0f, SideGrassHeightOffset, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));
                SideVertices.Add(SideVertices[SideVertices.Count - 2] + new Vector3(0.0f, -1.0f, 0.0f));

                Vector2[] UVSequence = new Vector2[]
                {
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.height),
                    new Vector2(SideGrassTextureRect.x, SideGrassTextureRect.y),
                    new Vector2(SideGrassTextureRect.width, SideGrassTextureRect.y)
                };

                SideUVs.Add(UVSequence[0]);
                SideUVs.Add(UVSequence[1]);
                SideUVs.Add(UVSequence[2]);
                SideUVs.Add(UVSequence[3]);

                float LightDirection = IsCurrentBlockShorter ? 1.0f : -1.0f;
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));

                // Then, Add the Dirt Block Side if needed
                if (Mathf.Abs(DownBlockHeight - BlockHeight) > 1)
                {
                    Vector3[] TempVertices = { SideVertices[SideVertices.Count - 2], SideVertices[SideVertices.Count - 1] };
                    int ResidualHeight = IsCurrentBlockShorter ? -(DownBlockHeight - BlockHeight - 1) : -(BlockHeight - DownBlockHeight - 1);
                    SideVertices.Add(TempVertices[0]);
                    SideVertices.Add(TempVertices[1]);
                    SideVertices.Add(TempVertices[0] + new Vector3(0.0f, ResidualHeight, 0.0f));
                    SideVertices.Add(TempVertices[1] + new Vector3(0.0f, ResidualHeight, 0.0f));

                    UVSequence = new Vector2[]
                    {
                    new Vector2(DirtTextureRect.x, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.height),
                    new Vector2(DirtTextureRect.x, DirtTextureRect.y),
                    new Vector2(DirtTextureRect.width, DirtTextureRect.y)
                    };

                    SideUVs.Add(UVSequence[0]);
                    SideUVs.Add(UVSequence[1]);
                    SideUVs.Add(UVSequence[2]);
                    SideUVs.Add(UVSequence[3]);

                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                    SideNormals.Add(new Vector3(0.0f, 0.0f, LightDirection));
                }
            }

            hx += 1;
            if (hx >= WorldData.ChunkSize)
            {
                hx = 0;
                hz += 1;
            }
        }

        int SideTriangleCount = 3 * (SideVertices.Count / 2);
        List<int> SideTriangles = new List<int>(SideTriangleCount);
        vi = QuadVertexCount;
        for (int i = 0; i < SideTriangleCount; i += 6)
        {
            SideTriangles.Add(vi + 0);
            SideTriangles.Add(vi + 1);
            SideTriangles.Add(vi + 2);

            SideTriangles.Add(vi + 1);
            SideTriangles.Add(vi + 2);
            SideTriangles.Add(vi + 3);

            vi += 4;
        }

        int TotalVertexCount = QuadVertexCount + SideVertices.Count;
        int TotalTriangleCount = QuadTriangleCount + SideTriangles.Count;
        Vector3[] TotalVertices = new Vector3[TotalVertexCount];
        Vector2[] TotalUVs = new Vector2[TotalVertexCount];
        Vector3[] TotalNormals = new Vector3[TotalVertexCount];
        int[] TotalTriangles = new int[TotalTriangleCount];
        int ti = 0;
        for (int i = 0; i < QuadVertexCount; ++i)
        {
            TotalVertices[ti] = QuadVertices[i];
            ++ti;
        }
        for (int i = 0; i < SideVertices.Count; ++i)
        {
            TotalVertices[ti] = SideVertices[i];
            ++ti;
        }
        ti = 0;
        for (int i = 0; i < QuadTriangleCount; ++i)
        {
            TotalTriangles[ti] = QuadTriangles[i];
            ++ti;
        }
        for (int i = 0; i < SideTriangles.Count; ++i)
        {
            TotalTriangles[ti] = SideTriangles[i];
            ++ti;
        }
        ti = 0;
        for (int i = 0; i < QuadVertexCount; ++i)
        {
            TotalUVs[ti] = QuadUVs[i];
            ++ti;
        }
        for (int i = 0; i < SideVertices.Count; ++i)
        {
            TotalUVs[ti] = SideUVs[i];
            ++ti;
        }
        ti = 0;
        for (int i = 0; i < QuadVertexCount; ++i)
        {
            TotalNormals[ti] = QuadNormals[i];
            ++ti;
        }
        for (int i = 0; i < SideVertices.Count; ++i)
        {
            TotalNormals[ti] = SideNormals[i];
            ++ti;
        }

        // Assign the Mesh Attributes
        ChunkMesh.vertices = TotalVertices;
        ChunkMesh.triangles = TotalTriangles;
        ChunkMesh.uv = TotalUVs;
        ChunkMesh.normals = TotalNormals;

        return ChunkMesh;
    }
}
