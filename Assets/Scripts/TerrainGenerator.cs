using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Profiling;
using Unity.Collections;
using Unity.Jobs;

public class TerrainGenerator : MonoBehaviour
{
    // Terrain Parameters
    //
    public int WorldLimitSize = 64000;
    // A Chunk is will group together a number of block ~ makes it easy to create and destroy on the fly
    public int ChunkSize = 32;
    // A Window of this Size is created around the player to know which chunks need to be loaded/unloaded
    public int ChunkLoadingWindowSize = 64;
    // A Window of this Size is created around the player to know which chunks need to be force loaded/unloaded (immediately)
    public int ChunkForceLoadingWindowSize = 32;
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
    public int MapToWorldScaleFactor = 1;

    // World Data
    private Grid UnityGrid;
    private NoiseGenerator.NoiseParameters NoiseParam;
    private Dictionary<Vector2Int, Chunk> ChunkMap;

    private int MaxNativeChunks = 16;
    private Dictionary<Vector2Int, ChunkJobHandle> ChunkGenJobHandleMap;
    
    // Indirect References for Chunk Generation Job
    public struct ChunkGenJobData
    {
        public static List<Grid> UnityGrid;
    }

    // Start is called before the first frame update
    void Start()
    {
        WorldData.WorldSize = WorldLimitSize;
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

        NoiseParam = new NoiseGenerator.NoiseParameters();
        NoiseParam.Reset();
        NoiseParam.NoiseScale = NoiseScale;
        NoiseParam.Octaves = Octaves;
        NoiseParam.Amplitude = Amplitude;
        NoiseParam.AmplitudeGain = AmplitudeGain;
        NoiseParam.Freq = Freq;
        NoiseParam.FreqGain = FreqGain;
        NoiseParam.PerlinShift = PerlinShift;
        NoiseParam.PerlinShiftGain = PerlinShiftGain;
        
        UnityGrid = GetComponent<Grid>();
        
        ChunkMap = new Dictionary<Vector2Int, Chunk>();
        
        if (ChunkForceLoadingWindowSize >= ChunkLoadingWindowSize)
        {
            ChunkForceLoadingWindowSize = ChunkLoadingWindowSize / 2;
        }
        ChunkGenJobHandleMap = new Dictionary<Vector2Int, ChunkJobHandle>();
        
        // Building Indirect References for Chunk Generation Job
        ChunkGenJobData.UnityGrid = new List<Grid>();
        ChunkGenJobData.UnityGrid.Add(UnityGrid);

        ChunkUtility.UnityGridRef = UnityGrid;

        // Loading Game Models
        BlockContainer.LoadModels();
    }

    // Update is called once per frame
    void Update()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
        // Run Any Necessary Tasks
        UnityMainThreadManager.GetInstance().Update();

        // Getting Loader Window
        WorldData.PlayerPosition = PlayerTransform.position;
        Vector2Int LoaderWindowStart = new Vector2Int(Mathf.FloorToInt(WorldData.PlayerPosition.x - ChunkLoadingWindowSize),
                                                      Mathf.FloorToInt(WorldData.PlayerPosition.z - ChunkLoadingWindowSize));
        Vector2Int LoaderWindowEnd = new Vector2Int(Mathf.FloorToInt(WorldData.PlayerPosition.x + ChunkLoadingWindowSize),
                                                    Mathf.FloorToInt(WorldData.PlayerPosition.z + ChunkLoadingWindowSize));

        // Normalizing Window Size to Chunk Size
        LoaderWindowStart /= WorldData.ChunkSize;
        LoaderWindowEnd /= WorldData.ChunkSize;

        Vector2Int CurrentChunkPosition = new Vector2Int();
        int NumOfChunks = 0;
        for (int i = LoaderWindowStart.y; i < LoaderWindowEnd.y; ++i)
        {
            for (int j = LoaderWindowStart.x; j < LoaderWindowEnd.x; ++j)
            {
                CurrentChunkPosition.x = j;
                CurrentChunkPosition.y = i;
                //Debug.Log(ChunkGenJobHandleMap.Count+ ", " + MaxNativeChunks);
                if (!WorldData.ActiveChunkSet.Contains(CurrentChunkPosition) &&
                    !ChunkGenJobHandleMap.ContainsKey(CurrentChunkPosition) &&
                    ChunkGenJobHandleMap.Count < MaxNativeChunks)
                {
                    // Creating a New Job
                    GenerateChunkJob Job = new GenerateChunkJob();
                    Job.ChunkArr = new NativeArray<Chunk>(1, Allocator.Persistent);
                    Job.Index = 0;
                    Job.StartPosition = CurrentChunkPosition * WorldData.ChunkSize;
                    Job.NoiseParam = NoiseParam;
                    Job.UnityGridIndex = ChunkGenJobData.UnityGrid.Count - 1;

                    // Generating a Handle for this Job
                    ChunkJobHandle NewHandle = new ChunkJobHandle();
                    NewHandle.NativeArrRef = Job.ChunkArr;
                    NewHandle.Handle = Job.Schedule();
                    ChunkGenJobHandleMap.Add(CurrentChunkPosition, NewHandle);

                    ++NumOfChunks;
                }
            }
        }

        Vector2Int ForceLoaderWindowStart = new Vector2Int(Mathf.FloorToInt(WorldData.PlayerPosition.x - ChunkForceLoadingWindowSize),
                                                      Mathf.FloorToInt(WorldData.PlayerPosition.z - ChunkForceLoadingWindowSize));
        Vector2Int ForceLoaderWindowEnd = new Vector2Int(Mathf.FloorToInt(WorldData.PlayerPosition.x + ChunkForceLoadingWindowSize),
                                                    Mathf.FloorToInt(WorldData.PlayerPosition.z + ChunkForceLoadingWindowSize));

        // Normalizing Window Size to Chunk Size
        ForceLoaderWindowStart /= WorldData.ChunkSize;
        ForceLoaderWindowEnd /= WorldData.ChunkSize;

        for (int i = ForceLoaderWindowStart.y; i < ForceLoaderWindowEnd.y; ++i)
        {
            for (int j = ForceLoaderWindowStart.x; j < ForceLoaderWindowEnd.x; ++j)
            {
                CurrentChunkPosition.x = j;
                CurrentChunkPosition.y = i;
                
                if (!WorldData.ActiveChunkSet.Contains(CurrentChunkPosition) &&
                    ChunkGenJobHandleMap.ContainsKey(CurrentChunkPosition))
                {
                    // Force Load this Chunk
                    ChunkJobHandle JobHandle = ChunkGenJobHandleMap[CurrentChunkPosition];
                    
                    if (JobHandle.Handle.IsCompleted)
                    {
                        JobHandle.Handle.Complete();

                        // Set it as Active
                        ChunkMap.Add(CurrentChunkPosition, JobHandle.NativeArrRef[0]);
                        WorldData.ActiveChunkSet.Add(CurrentChunkPosition);

                        JobHandle.NativeArrRef.Dispose();
                        ChunkGenJobHandleMap.Remove(CurrentChunkPosition);
                    }
                }

            }
        }

        // Destroy Chunks Out of Window
        string msg = "Removed: ";
        int Removed = 0;
        HashSet<Vector2Int> CurrentChunkSet = new HashSet<Vector2Int>(WorldData.ActiveChunkSet);
        foreach (var OldChunkPosition in CurrentChunkSet)
        {
            if (OldChunkPosition.x < LoaderWindowStart.x || OldChunkPosition.x > LoaderWindowEnd.x
                || OldChunkPosition.y < LoaderWindowStart.y || OldChunkPosition.y > LoaderWindowEnd.y)
            {
                Chunk OldChunk = ChunkMap[OldChunkPosition];
                ChunkUtility.Destroy(ref OldChunk);
                ChunkMap.Remove(OldChunkPosition);
                WorldData.ActiveChunkSet.Remove(OldChunkPosition);

                msg += OldChunkPosition + " | ";
                ++Removed;
            }
        }
        if (Removed > 0)
        {
            Debug.Log(msg);
        }
    }
}
