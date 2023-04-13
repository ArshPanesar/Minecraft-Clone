using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    // Terrain Parameters
    //
    public int WorldLimitSize = 64000;
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
    private NoiseGenerator.NoiseParameters NoiseParam;
    private Dictionary<Vector2Int, Chunk> ChunkMap;

    //private HashSet<Vector2Int> ActiveChunksInCurrTick;
    private HashSet<Vector2Int> ActiveChunksInPrevTick;

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
        //ActiveChunksInCurrTick = new HashSet<Vector2Int>();
        ActiveChunksInPrevTick = new HashSet<Vector2Int>();
    }

    // Update is called once per frame
    void Update()
    {
        GenerateTerrain();
    }

    public void GenerateTerrain()
    {
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
        for (int i = LoaderWindowStart.y; i < LoaderWindowEnd.y; ++i)
        {
            for (int j = LoaderWindowStart.x; j < LoaderWindowEnd.x; ++j)
            {
                CurrentChunkPosition.x = j;
                CurrentChunkPosition.y = i;
                if (!WorldData.ActiveChunkSet.Contains(CurrentChunkPosition))
                {
                    Chunk NewChunk = new Chunk();
                    NewChunk.Generate(CurrentChunkPosition * WorldData.ChunkSize, NoiseParam);
                    NewChunk.PlaceBlocks(UnityGrid);

                    ChunkMap.Add(CurrentChunkPosition, NewChunk);
                    WorldData.ActiveChunkSet.Add(CurrentChunkPosition);

                    ++Count;
                }
            }
        }

        //if (Count != 0)
        //{
        //    Debug.Log(Count);
        //}

        // Destroy Chunks Out of Window
        HashSet<Vector2Int> CurrentChunkSet = new HashSet<Vector2Int>(WorldData.ActiveChunkSet);
        foreach (var OldChunkPosition in CurrentChunkSet)
        {
            if (OldChunkPosition.x < LoaderWindowStart.x || OldChunkPosition.x > LoaderWindowEnd.x
                || OldChunkPosition.y < LoaderWindowStart.y || OldChunkPosition.y > LoaderWindowEnd.y)
            {
                ChunkMap[OldChunkPosition].Destroy();

                ChunkMap.Remove(OldChunkPosition);
                WorldData.ActiveChunkSet.Remove(OldChunkPosition);
            }
        }
    }
}
