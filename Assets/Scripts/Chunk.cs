using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public bool IsActive;

    private int[,] HeightMap;

    private Vector2Int Position;

    public GameObject ChunkGameObject;
    
    public int GetBlockHeight(Vector2Int GlobalBlockPosition, bool tryLookUpHeightMap = false)
    {
        // Early-Out
        if (tryLookUpHeightMap &&
            GlobalBlockPosition.x >= Position.x && GlobalBlockPosition.y >= Position.y &&
            GlobalBlockPosition.x < Position.x + WorldData.ChunkSize &&
            GlobalBlockPosition.y < Position.y + WorldData.ChunkSize )
        {
            int hx = GlobalBlockPosition.x - Position.x;
            int hy = GlobalBlockPosition.y - Position.y;

            if (HeightMap[hx, hy] != -1)
                return HeightMap[hx, hy];
        }

        // Else - Compute Height
        float x = (float)GlobalBlockPosition.x / (float)WorldData.WorldSmoothingFactor * WorldData.TerrainNoiseParam.NoiseScale;
        float y = (float)GlobalBlockPosition.y / (float)WorldData.WorldSmoothingFactor * WorldData.TerrainNoiseParam.NoiseScale;
        
        float noise = NoiseGenerator.FractalBrownianMotion(x, y, WorldData.TerrainNoiseParam);
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

        return height;
    }

    public Chunk()
    {
        IsActive = false;
        HeightMap = new int[WorldData.ChunkSize, WorldData.ChunkSize];
        for (int i = 0; i < WorldData.ChunkSize; ++i)
        {
            for (int j = 0; j < WorldData.ChunkSize; ++j)
                HeightMap[i, j] = -1;
        }

        Position = new Vector2Int(0, 0);

        ChunkGameObject = new GameObject("Chunk");
        ChunkGameObject.AddComponent<MeshFilter>();
        ChunkGameObject.AddComponent<MeshRenderer>();
    }

    public void Generate(Vector2Int StartPosition, Vector2Int EndPosition)
    {
        int hx = 0;//StartPosition.x - Position.x;
        int hy = 0;//StartPosition.y - Position.y;
        for (int i = StartPosition.y; i < EndPosition.y; ++i)
        {
            for (int j = StartPosition.x; j < EndPosition.x; ++j)
            {
                int height = GetBlockHeight(new Vector2Int(j, i));

                HeightMap[hx, hy] = height;
                ++hx;
            }

            hx = 0;//StartPosition.x - Position.x;
            ++hy;
        }
    }

    public void Destroy()
    {
        HeightMap = null;
        IsActive = false;

        GameObject.Destroy(ChunkGameObject);
    }


    // All Chunk Tasks Go Here
    //
    public class GenerateHeightMapTask : TaskManager.Task
    {
        public Chunk inout_ChunkRef;
        public Vector2Int in_StartPosition;

        public override void Execute()
        {
            inout_ChunkRef.Generate(in_StartPosition, in_StartPosition + new Vector2Int(WorldData.ChunkSize, WorldData.ChunkSize));
            inout_ChunkRef.Position = in_StartPosition;
        }
    }

    public class GenerateChunkMeshTask : TaskManager.Task
    {
        public Chunk inout_ChunkRef;
        public Vector2Int in_StartPosition;

        private bool CreatedMergeTask = false;
        private ChunkMeshGeneratorTask MeshGenTask;

        public override void Execute()
        {
            WaitingFlag = true;

            // Generate the Meshes
            if (!CreatedMergeTask)
            {
                MeshGenTask = new ChunkMeshGeneratorTask();
                MeshGenTask.inout_ChunkGameObject = inout_ChunkRef.ChunkGameObject;
                MeshGenTask.in_HeightMap = inout_ChunkRef.HeightMap;
                MeshGenTask.in_ChunkRef = inout_ChunkRef;
                MeshGenTask.in_Position = in_StartPosition;

                TaskManager.GetInstance().Enqueue(MeshGenTask);

                CreatedMergeTask = true;
            }

            if (!MeshGenTask.Completed)
            {
                return;
            }

         
            WaitingFlag = false;
        }
    }

    public class GenerateChunkTask : TaskManager.Task
    {
        public Vector2Int in_StartPosition;
        public NoiseGenerator.NoiseParameters in_NoiseParam;
        public Chunk inout_ChunkRef;
        public Grid in_UnityGrid;

        // Chunk Generation Parameters
        private bool CreatedGenerateTask = false;
        private bool CreatedGenerateChunkMeshTask = false;
        private GenerateHeightMapTask GenMapTask;
        private GenerateChunkMeshTask GenChunkMeshTask;

        public override void Execute()
        {
            WaitingFlag = true;

            if (!CreatedGenerateTask)
            {
                GenMapTask = new GenerateHeightMapTask();
                GenMapTask.inout_ChunkRef = inout_ChunkRef;
                GenMapTask.in_StartPosition = in_StartPosition;
                
                TaskManager.GetInstance().Enqueue(GenMapTask);

                CreatedGenerateTask = true;
            }

            // Wait for Generation to Complete
            if (!GenMapTask.Completed)
            {
                return;
            }
            // Map has been Generated

            // Merge all Blocks into a Single Mesh
            if (!CreatedGenerateChunkMeshTask)
            {
                GenChunkMeshTask = new GenerateChunkMeshTask();
                GenChunkMeshTask.inout_ChunkRef = inout_ChunkRef;
                GenChunkMeshTask.in_StartPosition = in_StartPosition;

                TaskManager.GetInstance().Enqueue(GenChunkMeshTask);
                
                CreatedGenerateChunkMeshTask = true;
            }

            // Wait for Chunk Meshes to be Created
            if (!GenChunkMeshTask.Completed)
            {
                return;
            }

            // Finally Set the Chunk to Active
            inout_ChunkRef.ChunkGameObject.transform.position = new Vector3(in_StartPosition.x, 0, in_StartPosition.y);
            inout_ChunkRef.IsActive = true;
            WaitingFlag = false;
        }
    }
}
