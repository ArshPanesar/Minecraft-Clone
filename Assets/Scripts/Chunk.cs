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

    public Chunk()
    {
        IsActive = false;
        HeightMap = new int[WorldData.ChunkSize, WorldData.ChunkSize];
        
        Position = new Vector2Int(0, 0);
        BlockContainer = new BlockContainer();
    }

    public void Generate(Vector2Int StartPosition, NoiseGenerator.NoiseParameters NoiseParam)
    {
        Position = StartPosition;

        Vector2Int EndPosition = StartPosition + (new Vector2Int(WorldData.ChunkSize, WorldData.ChunkSize));
        
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
                int height = (int)( ( (noise + 0.5f) / (0.5f - (-0.5f)) ) * ((float)WorldData.MaxHeight - (float)WorldData.MinHeight) + (float)WorldData.MinHeight );
                
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

    public void Destroy()
    {
        BlockContainer.ClearAll();

        BlockContainer = null;
        HeightMap = null;
        IsActive = false;
    }


    // All Chunk Tasks Go Here
    //
    public class GenerateHeightMapTask : TaskManager.Task
    {
        public Vector2Int in_StartPos;
        public NoiseGenerator.NoiseParameters in_NoiseParam;
        public Chunk inout_ChunkRef;

        public override void Execute()
        {
            inout_ChunkRef.Generate(in_StartPos, in_NoiseParam);
        }
    }

    public class PlaceGrassBlocksTask : TaskManager.Task
    {
        public Vector2Int in_StartPos;
        public int in_SizeToFill;
        public Grid in_UnityGrid;
        public Chunk inout_ChunkRef;
        public List<Vector3Int> inout_HighCellList;

        private bool IsHighCell(int x, int y)
        {
            int[,] HMap = inout_ChunkRef.HeightMap;
            int Height = HMap[x, y];

            List<Vector2Int> Neighbours = new List<Vector2Int>(8);

            bool IsEdgeBlock = false;

            // Check North
            if (y + 1 < WorldData.ChunkSize)
            {
                Neighbours.Add(new Vector2Int(x, y + 1));
            } 
            else { IsEdgeBlock = true; }
            
            // Check North-East
            if (x + 1 < WorldData.ChunkSize && y + 1 < WorldData.ChunkSize)
            {
                Neighbours.Add(new Vector2Int(x + 1, y + 1));
            }
            else { IsEdgeBlock = true; }
            
            // Check East
            if (x + 1 < WorldData.ChunkSize)
            {
                Neighbours.Add(new Vector2Int(x + 1, y));
            }
            else { IsEdgeBlock = true; }
            
            // Check South-East
            if (x + 1 < WorldData.ChunkSize && y - 1 > -1)
            {
                Neighbours.Add(new Vector2Int(x + 1, y - 1));
            }
            else { IsEdgeBlock = true; }

            // Check South
            if (y - 1 > -1)
            {
                Neighbours.Add(new Vector2Int(x, y - 1));
            }
            else { IsEdgeBlock = true; }

            // Check South-West
            if (x - 1 > -1 && y - 1 > -1)
            {
                Neighbours.Add(new Vector2Int(x - 1, y - 1));
            }
            else { IsEdgeBlock = true; }

            // Check West
            if (x - 1 > -1)
            {
                Neighbours.Add(new Vector2Int(x - 1, y));
            }
            else { IsEdgeBlock = true; }

            // Check North-West
            if (x - 1 > -1 && y + 1 < WorldData.ChunkSize)
            {
                Neighbours.Add(new Vector2Int(x - 1, y + 1));
            }
            else { IsEdgeBlock = true; }

            // Since we can't see the Chunks Neighbouring us, we fill all edge blocks
            if (IsEdgeBlock)
            {
                return true;
            }

            for (int i = 0; i < Neighbours.Count; ++i)
            {
                if (HMap[Neighbours[i].x, Neighbours[i].y] < Height - 1)
                {
                    return true;
                }
            }

            return false;
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
        public Chunk inout_ChunkRef;
        public Grid in_UnityGrid;

        public override void Execute()
        {
            BlockContainer BlockContainerRef = inout_ChunkRef.BlockContainer;

            for (int c = in_StartIndex; c < (in_StartIndex + in_Size); ++c)
            {
                Vector3Int cell = in_HighCellBlocks[c];
                for (int i = cell.y - Chunk.FillerBlocksDepth; i < cell.y; ++i)
                {
                    var block = BlockContainerRef.CreateBlock(BlockContainer.BlockID.DIRT);
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

    public class GenerateChunkTask : TaskManager.Task
    {
        public Vector2Int in_StartPosition;
        public NoiseGenerator.NoiseParameters in_NoiseParam;
        public Chunk inout_ChunkRef;
        public Grid in_UnityGrid;

        // Chunk Generation Parameters
        private bool CreatedGenerateTask = false;
        private bool CreatedPlaceBlocksTask = false;
        private GenerateHeightMapTask GenMapTask;
        private PlaceAllBlocksTask PlaceBlocksTask;

        public override void Execute()
        {
            WaitingFlag = true;

            if (!CreatedGenerateTask)
            {
                GenMapTask = new GenerateHeightMapTask();
                GenMapTask.inout_ChunkRef = inout_ChunkRef;
                GenMapTask.in_StartPos = in_StartPosition;
                GenMapTask.in_NoiseParam = in_NoiseParam;

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

            // Finally Set the Chunk to Active
            inout_ChunkRef.IsActive = true;
            WaitingFlag = false;
        }
    }
}
