using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    // Parameters for Chunk Tasks
    //
    // Place a Partial Chunk (Square Grid) Per Task
    public static int PlacePartialChunkPerTask = 8;
    // Number of Filler Blocks to Place per Task
    public static int PlaceFillerBlocksPerTask = 64;
    // Number of Filler Blocks to Place per Steep Hill
    public static int FillerBlocksDepth = 2;

    public bool IsActive;

    private int[,] HeightMap;

    private Vector2Int Position;
    private BlockContainer BlockContainer;

    // Single Mesh of Blocks
    // Optimized for Rendering
    private GameObject GrassMesh;
    private GameObject DirtMesh;

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
        BlockContainer = new BlockContainer();

        GrassMesh = BlockPool.GetInstance().CreateDummyBlock();
        DirtMesh = BlockPool.GetInstance().CreateDummyBlock();
    }

    public void Generate(Vector2Int StartPosition, Vector2Int EndPosition)
    {
        //Position = StartPosition;
        //Vector2Int EndPosition = StartPosition + (new Vector2Int(WorldData.ChunkSize, WorldData.ChunkSize));

        int hx = StartPosition.x - Position.x;
        int hy = StartPosition.y - Position.y;
        for (int i = StartPosition.y; i < EndPosition.y; ++i)
        {
            for (int j = StartPosition.x; j < EndPosition.x; ++j)
            {
                int height = GetBlockHeight(new Vector2Int(j, i));

                HeightMap[hx, hy] = height;
                ++hx;
            }

            hx = StartPosition.x - Position.x;
            ++hy;
        }
    }

    public void Destroy()
    {
        BlockContainer.ClearAll();

        BlockContainer = null;
        HeightMap = null;
        IsActive = false;

        GameObject.Destroy(GrassMesh);
        GameObject.Destroy(DirtMesh);
        GrassMesh = null;
        DirtMesh = null;
    }


    // All Chunk Tasks Go Here
    //
    public class GeneratePartialHeightMapTask : TaskManager.Task
    {
        public Vector2Int in_StartPos;
        public Vector2Int in_EndPos;
        public Chunk inout_ChunkRef;

        public override void Execute()
        {
            inout_ChunkRef.Generate(in_StartPos, in_EndPos);
        }
    }

    public class GenerateHeightMapTask : TaskManager.Task
    {
        public Vector2Int in_StartPos;
        public Chunk inout_ChunkRef;

        public List<GeneratePartialHeightMapTask> PartialChunkTaskList;

        public bool CreatedGenPartialChunkTasks = false;

        public override void Execute()
        {
            WaitingFlag = true;

            int numOfPartialChunks = (WorldData.ChunkSize / PlacePartialChunkPerTask) * (WorldData.ChunkSize / PlacePartialChunkPerTask);
            
            if (!CreatedGenPartialChunkTasks)
            {
                // Set Chunk Position
                inout_ChunkRef.Position = in_StartPos;

                // Generate Height Map
                PartialChunkTaskList = new List<GeneratePartialHeightMapTask>();

                Vector2Int StartPosition = in_StartPos;
                Vector2Int EndPosition = StartPosition + new Vector2Int(PlacePartialChunkPerTask, PlacePartialChunkPerTask);
                for (int i = 0; i < numOfPartialChunks; ++i)
                {
                    var NewTask = new GeneratePartialHeightMapTask();
                    NewTask.in_StartPos = StartPosition;
                    NewTask.in_EndPos = EndPosition;
                    NewTask.inout_ChunkRef = inout_ChunkRef;

                    PartialChunkTaskList.Add(NewTask);
                    TaskManager.GetInstance().Enqueue(NewTask);

                    StartPosition.x += PlacePartialChunkPerTask;
                    if ((i + 1) % (WorldData.ChunkSize / PlacePartialChunkPerTask) == 0)
                    {
                        StartPosition.x = in_StartPos.x;
                        StartPosition.y += PlacePartialChunkPerTask;
                    }

                    EndPosition = StartPosition + new Vector2Int(PlacePartialChunkPerTask, PlacePartialChunkPerTask);
                }

                CreatedGenPartialChunkTasks = true;
            }

            for (int i = 0; i < PartialChunkTaskList.Count; ++i)
            {
                if (!PartialChunkTaskList[i].Completed)
                    return;
            }

            // Completed
            WaitingFlag = false;
        }
    }

    public class PlaceGrassBlocksTask : TaskManager.Task
    {
        public Vector2Int in_StartPos;
        public int in_SizeToFill;
        public Grid in_UnityGrid;
        public Chunk inout_ChunkRef;
        public List<Vector3Int> inout_HighCellList;
        public List<int> inout_DepthList;

        private bool IsHighCell(int x, int y)
        {
            int[,] HMap = inout_ChunkRef.HeightMap;
            int Height = HMap[x, y];

            List<int> NeighbourHeightList = new List<int>(8);

            Vector2Int ChunkGlobalPosition = inout_ChunkRef.Position;
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x, y + 1), true) );
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x, y - 1), true) );
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x - 1, y), true) );
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x + 1, y), true) );
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x - 1, y + 1), true));       
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x - 1, y - 1), true));           
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x + 1, y - 1), true));           
            NeighbourHeightList.Add( inout_ChunkRef.GetBlockHeight(ChunkGlobalPosition + new Vector2Int(x + 1, y + 1), true));

            bool IsHigh = false;
            int MaxFillDepth = 0;
            for (int i = 0; i < NeighbourHeightList.Count; ++i)
            {
                if (NeighbourHeightList[i] < Height - 1)
                {
                    MaxFillDepth = Mathf.Max(MaxFillDepth, Mathf.Abs(Height - NeighbourHeightList[i]));
                    IsHigh = true;
                }
            }

            if (IsHigh) { inout_DepthList.Add(MaxFillDepth); }
            return IsHigh;
        }

        public override void Execute()
        {
            Vector2Int Position = inout_ChunkRef.Position;
            BlockContainer BlockContainerRef = inout_ChunkRef.BlockContainer;
            int[,] HeightMap = inout_ChunkRef.HeightMap;

            for (int i = in_StartPos.y; i < (in_StartPos.y + in_SizeToFill); i++)
            {
                for (int j = in_StartPos.x; j < (in_StartPos.x + in_SizeToFill); j++)
                {
                    int x = Position.x + j;
                    int y = HeightMap[j, i];
                    int z = Position.y + i;

                    var block = BlockContainerRef.CreateBlock();

                    block.transform.position = in_UnityGrid.CellToWorld(new Vector3Int(x, y, z));
                    if (IsHighCell(j, i))
                    {
                        inout_HighCellList.Add(new Vector3Int(x, y, z));
                    }
                }
            }
        }
    }

    public class FillEmptyBlocksTask : TaskManager.Task
    {
        public int in_StartIndex;
        public int in_Size;
        public List<Vector3Int> in_HighCellBlocks;
        public List<int> in_DepthList;
        public Chunk inout_ChunkRef;
        public Grid in_UnityGrid;

        public override void Execute()
        {
            BlockContainer BlockContainerRef = inout_ChunkRef.BlockContainer;

            for (int c = in_StartIndex; c < (in_StartIndex + in_Size); ++c)
            {
                Vector3Int cell = in_HighCellBlocks[c];
                int FillDepth = in_DepthList[c];
                for (int i = cell.y - FillDepth; i < cell.y; ++i)
                {
                    var block = BlockContainerRef.CreateBlock(BlockID.DIRT);
                    block.transform.position = in_UnityGrid.CellToWorld(new Vector3Int(cell.x, i, cell.z));
                }
            }
        }
    }

    public class PlaceAllBlocksTask : TaskManager.Task
    {
        public Chunk inout_ChunkRef;
        public Grid in_UnityGrid;

        private List<PlaceGrassBlocksTask> PlaceBlocksTaskList = new List<PlaceGrassBlocksTask>();
        private List<Vector3Int> HighCellList = new List<Vector3Int>();
        private List<int> FillDepthList = new List<int>();
        
        private List<FillEmptyBlocksTask> FillEmptyTaskList = new List<FillEmptyBlocksTask>();
        
        private bool CreatedPlaceGrassBlockTasks = false;
        private bool CreatedPlaceFillerBlockTasks = false;

        private void CreatePlaceGrassBlockTasks()
        {
            int NumOfTasksRequired = (WorldData.ChunkSize * WorldData.ChunkSize) / (PlacePartialChunkPerTask * PlacePartialChunkPerTask);
            int StartX = 0;
            int StartY = 0;
            for (int i = 0; i < NumOfTasksRequired; ++i)
            {
                StartX = (i * PlacePartialChunkPerTask);
                if (StartX >= WorldData.ChunkSize)
                {
                    StartX = StartX % WorldData.ChunkSize;
                    if (StartX == 0)
                    {
                        StartY += PlacePartialChunkPerTask;
                    }
                }

                PlaceBlocksTaskList.Add(new PlaceGrassBlocksTask());
                PlaceGrassBlocksTask NewTask = PlaceBlocksTaskList[PlaceBlocksTaskList.Count - 1];
                NewTask.in_UnityGrid = in_UnityGrid;
                NewTask.inout_HighCellList = HighCellList;
                NewTask.inout_DepthList = FillDepthList;
                NewTask.inout_ChunkRef = inout_ChunkRef;
                NewTask.in_StartPos = new Vector2Int(StartX, StartY);
                NewTask.in_SizeToFill = PlacePartialChunkPerTask;

                TaskManager.GetInstance().Enqueue(NewTask);
            }
        }

        private void CreateFillerBlocksTasks()
        {
            int NumOfTasksRequired = Mathf.CeilToInt((float)(HighCellList.Count * FillerBlocksDepth) / (float)(PlaceFillerBlocksPerTask));
            for (int i = 0; i < NumOfTasksRequired; ++i)
            {
                int StartIndex = i * PlaceFillerBlocksPerTask;
                int Size = PlaceFillerBlocksPerTask;
                if ((StartIndex + Size) > HighCellList.Count)
                {
                    Size = HighCellList.Count - StartIndex;
                }

                FillEmptyTaskList.Add(new FillEmptyBlocksTask());
                FillEmptyBlocksTask NewTask = FillEmptyTaskList[FillEmptyTaskList.Count - 1];
                NewTask.in_UnityGrid = in_UnityGrid;
                NewTask.in_HighCellBlocks = HighCellList;
                NewTask.in_DepthList = FillDepthList;
                NewTask.inout_ChunkRef = inout_ChunkRef;
                NewTask.in_StartIndex = StartIndex;
                NewTask.in_Size = Size;

                TaskManager.GetInstance().Enqueue(NewTask);
            }
        }

        public override void Execute()
        {
            WaitingFlag = true;
            if (!CreatedPlaceGrassBlockTasks)
            {
                CreatePlaceGrassBlockTasks();

                CreatedPlaceGrassBlockTasks = true;
            }

            // Wait For All Tasks to be Finished
            for (int i = 0; i < PlaceBlocksTaskList.Count; ++i)
            {
                if (!PlaceBlocksTaskList[i].Completed)
                {
                    return;
                }
            }

            // All Initial Blocks Filled
            //
            // Fill Empty Areas
            if (!CreatedPlaceFillerBlockTasks)
            {
                CreateFillerBlocksTasks();

                CreatedPlaceFillerBlockTasks = true;
            }

            // Wait For All Tasks to be Finished
            for (int i = 0; i < FillEmptyTaskList.Count; ++i)
            {
                if (!FillEmptyTaskList[i].Completed)
                {
                    return;
                }
            }

            WaitingFlag = false;
        }
    }

    public class GenerateChunkMeshTask : TaskManager.Task
    {
        public Chunk inout_ChunkRef;

        private bool GrassMeshCreated = false;
        private bool DirtMeshCreated = false;

        public override void Execute()
        {
            WaitingFlag = true;

            //
            // Grass Mesh and Dirt Mesh are created in Separate Frames
            //

            if (!GrassMeshCreated) 
            {
                inout_ChunkRef.GrassMesh.GetComponent<MeshFilter>().sharedMesh = inout_ChunkRef.BlockContainer.MergeIntoSingleMesh(BlockID.GRASS);
                inout_ChunkRef.GrassMesh.SetActive(true);
                GrassMeshCreated = true;
                return;
            }

            if (!DirtMeshCreated)
            {
                inout_ChunkRef.DirtMesh.GetComponent<MeshFilter>().sharedMesh = inout_ChunkRef.BlockContainer.MergeIntoSingleMesh(BlockID.DIRT);
                inout_ChunkRef.DirtMesh.SetActive(true);
                DirtMeshCreated = true;
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
        private bool CreatedPlaceBlocksTask = false;
        private bool CreatedGenerateChunkMeshTask = false;
        private GenerateHeightMapTask GenMapTask;
        private PlaceAllBlocksTask PlaceBlocksTask;
        private GenerateChunkMeshTask GenChunkMeshTask;

        public override void Execute()
        {
            WaitingFlag = true;

            if (!CreatedGenerateTask)
            {
                GenMapTask = new GenerateHeightMapTask();
                GenMapTask.inout_ChunkRef = inout_ChunkRef;
                GenMapTask.in_StartPos = in_StartPosition;
                
                TaskManager.GetInstance().Enqueue(GenMapTask);

                CreatedGenerateTask = true;
            }

            // Wait for Generation to Complete
            if (!GenMapTask.Completed)
            {
                return;
            }
            // Map has been Generated

            if (!CreatedPlaceBlocksTask)
            {
                PlaceBlocksTask = new PlaceAllBlocksTask();
                PlaceBlocksTask.inout_ChunkRef = inout_ChunkRef;
                PlaceBlocksTask.in_UnityGrid = in_UnityGrid;

                TaskManager.GetInstance().Enqueue(PlaceBlocksTask);

                CreatedPlaceBlocksTask = true;
            }

            if (!PlaceBlocksTask.Completed)
            {
                return;
            }
            // Chunk Generation Complete

            // Merge all Blocks into a Single Mesh
            if (!CreatedGenerateChunkMeshTask)
            {
                GenChunkMeshTask = new GenerateChunkMeshTask();
                GenChunkMeshTask.inout_ChunkRef = inout_ChunkRef;

                TaskManager.GetInstance().Enqueue(GenChunkMeshTask);
                
                CreatedGenerateChunkMeshTask = true;
            }

            // Wait for Chunk Meshes to be Created
            if (!GenChunkMeshTask.Completed)
            {
                return;
            }

            // Finally Set the Chunk to Active
            inout_ChunkRef.IsActive = true;
            WaitingFlag = false;
        }
    }
}
