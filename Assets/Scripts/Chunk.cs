using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;



public struct Chunk
{
    public bool IsActive;
    
    public int PositionX;
    public int PositionY;

    // Chunk needs to be Unmanaged for it to be used in a Native Container
    // Holding References to Managed Types
    public int HeightMapIndex;
    public int BlockContainerIndex;

    public bool Initialized;
}

public class ChunkUtility
{
    public static void Reset(Chunk ChunkRef)
    {
        if (!ChunkRef.Initialized)
        {
            ChunkRef.BlockContainerIndex = -1;

            ChunkRef.Initialized = true;
        }

        Destroy(ChunkRef);

        ChunkRef.IsActive = false;
        
        //Position = new Vector2Int(0, 0);
        ChunkRef.PositionX = 0;
        ChunkRef.PositionY = 0;

        int Index = WorldData.ManagedChunkData.CreateData();
        ChunkRef.BlockContainerIndex = Index;
        ChunkRef.HeightMapIndex = Index;
    }

    public static void Generate(Chunk ChunkRef, Vector2Int StartPosition, NoiseGenerator.NoiseParameters NoiseParam)
    {
        ChunkRef.IsActive = true;
        //Position = StartPosition;
        ChunkRef.PositionX = StartPosition.x;
        ChunkRef.PositionY = StartPosition.y;

        Vector2Int EndPosition = StartPosition + (new Vector2Int(WorldData.ChunkSize, WorldData.ChunkSize));

        int[,] HeightMap = WorldData.ManagedChunkData.HeightMapDict[ChunkRef.HeightMapIndex];
        int hx = 0, hy = 0;
        for (int i = StartPosition.y; i < EndPosition.y; ++i)
        {
            for (int j = StartPosition.x; j < EndPosition.x; ++j)
            {
                float y = (float)i / (float)WorldData.WorldSmoothingFactor * NoiseParam.NoiseScale;
                float x = (float)j / (float)WorldData.WorldSmoothingFactor * NoiseParam.NoiseScale;

                float noise = NoiseGenerator.FractalBrownianMotion(x, y, NoiseParam);
                noise = Mathf.Clamp(noise, -0.5f, 0.5f);

                // Scaling Height from [-1, 1] Range
                int height = (int)(((noise + 0.5f) / (0.5f - (-0.5f))) * ((float)WorldData.MaxHeight - (float)WorldData.MinHeight) + (float)WorldData.MinHeight);

                if (height < 20)
                {
                    height = 20;
                }
                else if (height < 25)
                {
                    height = 21;
                }
                else if (height < 30)
                {
                    height = 22;
                }
                else
                {
                    height -= 7;
                }

                HeightMap[hx, hy] = height;
                ++hx;
            }

            hx = 0;
            ++hy;
        }
    }
    static readonly ProfilerMarker s_ProfMarker1 = new ProfilerMarker("PlaceBlocks() [Generating Blocks]");
    static readonly ProfilerMarker s_ProfMarker2 = new ProfilerMarker("PlaceBlocks() [Filling Dirt Blocks]");

    public static void PlaceBlocks(Chunk ChunkRef, Grid UnityGrid)
    {
        //s_ProfMarker1.Begin();

        // Generate the Actual Blocks
        //Debug.Log("IndexL: " + ChunkRef.BlockContainerIndex);
        //Debug.Log("Size: " + WorldData.ManagedChunkData.BlockContainerList);
        BlockContainer BlockContainerRef = WorldData.ManagedChunkData.BlockContainerDict[ChunkRef.BlockContainerIndex];
        int[,] HeightMap = WorldData.ManagedChunkData.HeightMapDict[ChunkRef.HeightMapIndex];
        List<Vector3Int> high_cell_list = new List<Vector3Int>();
        for (int i = 0; i < WorldData.ChunkSize; i++)
        {
            for (int j = 0; j < WorldData.ChunkSize; j++)
            {
                int x = ChunkRef.PositionX + j * WorldData.MapToWorldScaleFactor;
                int y = HeightMap[j, i] * WorldData.MapToWorldScaleFactor;
                int z = ChunkRef.PositionY + i * WorldData.MapToWorldScaleFactor;


                var block = BlockContainerRef.CreateBlock();


                //Debug.Log(new Vector3Int(x, y, z) + " ? " + UnityGrid.CellToWorld(new Vector3Int(x, y, z)));
                block.transform.position = UnityGrid.CellToWorld(new Vector3Int(x, y, z));

                if (HeightMap[j, i] > WorldData.MinHeight)
                {
                    high_cell_list.Add(new Vector3Int(x, y, z));
                }
            }
        }
        //s_ProfMarker1.End();


        //s_ProfMarker2.Begin();

        // Fill Empty
        foreach (var cell in high_cell_list)
        {
            for (int i = cell.y - 4; i < cell.y; ++i)
            {
                var block = BlockContainerRef.CreateBlock(BlockContainer.BlockID.DIRT);
                block.transform.position = UnityGrid.CellToWorld(new Vector3Int(cell.x, i, cell.z));
            }
        }
        //s_ProfMarker2.End();

        //BlockContainer.GenerateRenderBatches();
    }

    public static void Destroy(Chunk ChunkRef)
    {
        if (ChunkRef.BlockContainerIndex != -1)
        {
            WorldData.ManagedChunkData.DestroyData(ChunkRef.BlockContainerIndex);

            ChunkRef.BlockContainerIndex = -1;
            ChunkRef.HeightMapIndex = -1;
        }
        ChunkRef.IsActive = false;
    }
}

public struct GenerateChunkJob : IJob
{
    public NativeArray<Chunk> ChunkArr;
    public int Index;

    public Vector2Int StartPosition;
    public NoiseGenerator.NoiseParameters NoiseParam;
    public int UnityGridIndex;

    public void Execute()
    {
        var NewChunk = ChunkArr[Index];

        ChunkUtility.Reset(NewChunk);

        ChunkUtility.Generate(NewChunk, StartPosition, NoiseParam);
        ChunkUtility.PlaceBlocks(NewChunk, TerrainGenerator.ChunkGenJobData.UnityGrid[UnityGridIndex]);
    }
}

public class ChunkJobHandle 
{
    public NativeArray<Chunk> NativeArrRef;
    public JobHandle Handle;
}
