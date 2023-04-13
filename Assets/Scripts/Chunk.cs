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
    public int ManagedDataIndex;

    public bool Initialized;
}

public class ChunkUtility
{
    public static Grid UnityGridRef;

    public static void Reset(ref Chunk ChunkRef)
    {
        if (!ChunkRef.Initialized)
        {
            ChunkRef.ManagedDataIndex = -1;

            ChunkRef.Initialized = true;
        }

        Destroy(ref ChunkRef);

        ChunkRef.IsActive = false;
        
        ChunkRef.PositionX = 0;
        ChunkRef.PositionY = 0;

        ChunkRef.ManagedDataIndex = WorldData.ManagedChunkData.CreateData();
        //Debug.Log(ChunkRef.ManagedDataIndex);
    }

    public static void Generate(ref Chunk ChunkRef, Vector2Int StartPosition, NoiseGenerator.NoiseParameters NoiseParam)
    {
        ChunkRef.IsActive = true;
        ChunkRef.PositionX = StartPosition.x;
        ChunkRef.PositionY = StartPosition.y;

        Vector2Int EndPosition = StartPosition + (new Vector2Int(WorldData.ChunkSize, WorldData.ChunkSize));
        //Debug.Log(ChunkRef.ManagedDataIndex);
        int[,] HeightMap = WorldData.ManagedChunkData.HeightMapDict[ChunkRef.ManagedDataIndex];
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

    public static void PlaceBlocks(ref Chunk ChunkRef)
    {
        // Generate the Actual Blocks
        BlockContainer BlockContainerRef = WorldData.ManagedChunkData.BlockContainerDict[ChunkRef.ManagedDataIndex];
        int[,] HeightMap = WorldData.ManagedChunkData.HeightMapDict[ChunkRef.ManagedDataIndex];
        List<Vector3Int> high_cell_list = new List<Vector3Int>();
        
        // Calling Main Thread to Create Blocks
        BlockContainer.CreateBlocksTask CreateBlocksTask = new BlockContainer.CreateBlocksTask();
        CreateBlocksTask.in_BlockID = BlockContainer.BlockID.GRASS;
        CreateBlocksTask.in_NumOfBlocks = WorldData.ChunkSize * WorldData.ChunkSize;
        CreateBlocksTask.inout_BlockContainerRef = BlockContainerRef;

        // Waiting for the Task to be Finished
        UnityMainThreadManager.GetInstance().Enqueue(CreateBlocksTask);
        while (!CreateBlocksTask.Completed) { };

        List<Vector3Int> PosList = new List<Vector3Int>();
        for (int i = 0; i < WorldData.ChunkSize; i++)
        {
            for (int j = 0; j < WorldData.ChunkSize; j++)
            {
                int x = ChunkRef.PositionX + j * WorldData.MapToWorldScaleFactor;
                int y = HeightMap[j, i] * WorldData.MapToWorldScaleFactor;
                int z = ChunkRef.PositionY + i * WorldData.MapToWorldScaleFactor;


                //var block = BlockContainerRef.GetBlock(i * WorldData.ChunkSize + j);

                //block.transform.position = UnityGridRef.CellToWorld(new Vector3Int(x, y, z));
                PosList.Add(new Vector3Int(x, y, z));
                if (HeightMap[j, i] > WorldData.MinHeight)
                {
                    high_cell_list.Add(new Vector3Int(x, y, z));
                }
            }
        }

        BlockContainer.PlaceBlocksTask PlaceBlocksTask = new BlockContainer.PlaceBlocksTask();
        PlaceBlocksTask.in_PosList = PosList;
        PlaceBlocksTask.inout_BlockContainerRef = BlockContainerRef;
        PlaceBlocksTask.in_UnityGrid = UnityGridRef;
        PlaceBlocksTask.in_StartIndex = 0;
        PlaceBlocksTask.in_NumOfBlocks = WorldData.ChunkSize * WorldData.ChunkSize;

        UnityMainThreadManager.GetInstance().Enqueue(PlaceBlocksTask);


        CreateBlocksTask = new BlockContainer.CreateBlocksTask();
        CreateBlocksTask.in_BlockID = BlockContainer.BlockID.DIRT;
        CreateBlocksTask.in_NumOfBlocks = high_cell_list.Count * 4;
        CreateBlocksTask.inout_BlockContainerRef = BlockContainerRef;

        // Waiting for the Task to be Finished
        int LastBlockAdded = BlockContainerRef.BlockList.Count;
        UnityMainThreadManager.GetInstance().Enqueue(CreateBlocksTask);
        while (!CreateBlocksTask.Completed) { };

        // Fill Empty
        BlockContainer.PlaceBlocksTask PlaceFillerBlocksTask = new BlockContainer.PlaceBlocksTask();
        PlaceFillerBlocksTask.in_PosList = new List<Vector3Int>();
        PlaceFillerBlocksTask.inout_BlockContainerRef = BlockContainerRef;
        PlaceFillerBlocksTask.in_UnityGrid = UnityGridRef;
        PlaceFillerBlocksTask.in_StartIndex = LastBlockAdded;
        PlaceFillerBlocksTask.in_NumOfBlocks = high_cell_list.Count * 4;

        foreach (var cell in high_cell_list)
        {
            for (int i = cell.y - 4; i < cell.y; ++i)
            {
                PlaceFillerBlocksTask.in_PosList.Add(new Vector3Int(cell.x, i, cell.z));
            }
        }

        UnityMainThreadManager.GetInstance().Enqueue(PlaceFillerBlocksTask);

        while(!PlaceBlocksTask.Completed) { };
        while (!PlaceFillerBlocksTask.Completed) { };
    }

    public static void Destroy(ref Chunk ChunkRef)
    {
        if (ChunkRef.ManagedDataIndex != -1)
        {
            WorldData.ManagedChunkData.DestroyData(ChunkRef.ManagedDataIndex);

            ChunkRef.ManagedDataIndex = -1;
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
        Debug.Log("STARTING");

        var NewChunk = ChunkArr[Index];

        Debug.Log("RESET");

        ChunkUtility.Reset(ref NewChunk);
        //Debug.Log(NewChunk.ManagedDataIndex);
        Debug.Log("GENERATE");

        ChunkUtility.Generate(ref NewChunk, StartPosition, NoiseParam);
        Debug.Log("PLACE");

        ChunkUtility.PlaceBlocks(ref NewChunk);

        Debug.Log("ENDED");

    }
}

public class ChunkJobHandle 
{
    public NativeArray<Chunk> NativeArrRef;
    public JobHandle Handle;
}
