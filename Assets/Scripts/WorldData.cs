using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldData
{
    public static int WorldSize = 64000;
    public static int ChunkSize = 32;
    public static int WorldSmoothingFactor = 200;
    public static int NumOfChunks = WorldSize / ChunkSize;

    public static int MinHeight = 1;
    public static int MaxHeight = 64;

    public static int MapToWorldScaleFactor = 1;

    public static Dictionary<Vector2Int, Chunk> ChunkMap;

    public static NoiseGenerator.NoiseParameters TerrainNoiseParam;

    public static Vector3 PlayerPosition;

    public static Material TextureAtlasMaterial;

    static WorldData()
    {
        Evaluate();
    }

    public static void Evaluate()
    {
        NumOfChunks = WorldSize / ChunkSize;
        
        ChunkMap = new Dictionary<Vector2Int, Chunk>();

        TerrainNoiseParam = new NoiseGenerator.NoiseParameters();

        PlayerPosition = Vector3.zero;

        TextureAtlasMaterial = Resources.Load<Material>("Materials/texture_atlas");
    }
}
