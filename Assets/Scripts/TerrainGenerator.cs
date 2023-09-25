using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    // Terrain Parameters
    //
    // A Chunk is will group together a number of block ~ makes it easy to create and destroy on the fly
    public int ChunkSize = 32;
    // A Window of this Size is created around the player to know which chunks need to be loaded/unloaded
    public int ChunkLoadingWindowSize = 64;
    // Smoothing Factor will help in smoothing out the terrain height values
    public int WorldSmoothingFactor = 200;

    // Player
    public Transform PlayerTransform;
    
    // Noise Function Parameters
    public float NoiseScale = 3.5f;
    public int Octaves = 6;
    public float Amplitude = 0.5f;
    public float AmplitudeGain = 0.5f;
    public float Freq = 1.0f;
    public float FreqGain = 2.0f;
    public float PerlinShift = 0.0f;
    public float PerlinShiftGain = 2.0f;

    // Procedural Generation Factors
    public int MaxHeight = 64;
    public int MinHeight = 1;
    public float LandFilter01 = 0.0f; // Noise greater than this value will be Land
    public int MapToWorldScaleFactor = 1;


    // World Data
    private Grid UnityGrid;
    private HashSet<Vector2Int> ActiveChunksInPrevTick;

    // GameObjects of Blocks
    public GameObject GrassBlock;
    public GameObject DirtBlock;

    // Start is called before the first frame update
    void Start()
    {
        // Set Block Data
        GrassBlock = GrassBlock.transform.GetChild(0).gameObject;
        DirtBlock = DirtBlock.transform.GetChild(0).gameObject;
        BlockContainer.DirtBlock = DirtBlock;
        BlockContainer.GrassBlock = GrassBlock;

        // Set World Data
        WorldData.ChunkSize = ChunkSize;
        WorldData.MinHeight = MinHeight;
        WorldData.MaxHeight = MaxHeight;
        WorldData.MapToWorldScaleFactor = MapToWorldScaleFactor;
        WorldData.WorldSmoothingFactor = WorldSmoothingFactor;

        if (PlayerTransform == null)
        {
            Debug.Log("Warning: PlayerTransform is null. Assuming Position (0, 0).");
            WorldData.PlayerPosition = Vector2.zero;
        }
        else
        {
            WorldData.PlayerPosition = PlayerTransform.position;
        }

        WorldData.Evaluate();

        WorldData.TerrainNoiseParam = new NoiseGenerator.NoiseParameters();
        WorldData.TerrainNoiseParam.NoiseScale = NoiseScale;
        WorldData.TerrainNoiseParam.Octaves = Octaves;
        WorldData.TerrainNoiseParam.Amplitude = Amplitude;
        WorldData.TerrainNoiseParam.AmplitudeGain = AmplitudeGain;
        WorldData.TerrainNoiseParam.Freq = Freq;
        WorldData.TerrainNoiseParam.FreqGain = FreqGain;
        WorldData.TerrainNoiseParam.PerlinShift = PerlinShift;
        WorldData.TerrainNoiseParam.PerlinShiftGain = PerlinShiftGain;

        UnityGrid = GetComponent<Grid>();
        //ActiveChunksInCurrTick = new HashSet<Vector2Int>();
        ActiveChunksInPrevTick = new HashSet<Vector2Int>();

        Debug.Log("GPU Device: " + SystemInfo.graphicsDeviceName);
    }

    void Update()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        // Complete Tasks
        TaskManager.GetInstance().Update();

        // Getting Loader Window
        WorldData.PlayerPosition = PlayerTransform.position;
        Vector2Int LoaderWindowStart = new Vector2Int(Mathf.FloorToInt(WorldData.PlayerPosition.x - ChunkLoadingWindowSize),
                                                      Mathf.FloorToInt(WorldData.PlayerPosition.z - ChunkLoadingWindowSize));
        Vector2Int LoaderWindowEnd = new Vector2Int(Mathf.FloorToInt(WorldData.PlayerPosition.x + ChunkLoadingWindowSize),
                                                    Mathf.FloorToInt(WorldData.PlayerPosition.z + ChunkLoadingWindowSize));

        // Normalizing Window Size to Chunk Size
        LoaderWindowStart /= WorldData.ChunkSize;
        LoaderWindowEnd /= WorldData.ChunkSize;
        
        
        int Count = 0;
        Vector2Int CurrentChunkPosition = new Vector2Int();
        List<Chunk.GenerateChunkTask> ChunkTaskList = new List<Chunk.GenerateChunkTask>(8);
        for (int i = LoaderWindowStart.y; i < LoaderWindowEnd.y; ++i)
        {
            for (int j = LoaderWindowStart.x; j < LoaderWindowEnd.x; ++j)
            {
                CurrentChunkPosition.x = j;
                CurrentChunkPosition.y = i;
                if (!WorldData.ChunkMap.ContainsKey(CurrentChunkPosition))
                {
                    Chunk NewChunk = new Chunk();
                    
                    Chunk.GenerateChunkTask NewTask = new Chunk.GenerateChunkTask();
                    NewTask.in_StartPosition = CurrentChunkPosition * WorldData.ChunkSize;
                    NewTask.inout_ChunkRef = NewChunk;
                    NewTask.in_UnityGrid = UnityGrid;

                    ChunkTaskList.Add(NewTask);
                    //TaskManager.GetInstance().Enqueue(NewTask);

                    WorldData.ChunkMap.Add(CurrentChunkPosition, NewChunk);
                    
                    ++Count;
                }
            }
        }

        for (int i = 0; i < ChunkTaskList.Count; ++i)
        {
            TaskManager.GetInstance().Enqueue(ChunkTaskList[i]);
        }

        // Destroy Chunks Out of Window
        List<Vector2Int> RemoveChunkList = new List<Vector2Int>(WorldData.ChunkMap.Keys.Count);
        foreach (var OldChunkPosition in WorldData.ChunkMap.Keys)
        {
            if (OldChunkPosition.x < LoaderWindowStart.x || OldChunkPosition.x > LoaderWindowEnd.x
                || OldChunkPosition.y < LoaderWindowStart.y || OldChunkPosition.y > LoaderWindowEnd.y)
            {
                if (WorldData.ChunkMap[OldChunkPosition].IsActive)
                {
                    WorldData.ChunkMap[OldChunkPosition].Destroy();

                    RemoveChunkList.Add(OldChunkPosition);
                }
            }
        }
        foreach (var Pos in RemoveChunkList)
        {
            WorldData.ChunkMap.Remove(Pos);
        }
    }

    public void RemoveAllChunks()
    {
        // Delete All Chunks
        foreach (var Chunk in WorldData.ChunkMap.Values)
        {
            Chunk.Destroy();
        }
        WorldData.ChunkMap.Clear();
    }
}
