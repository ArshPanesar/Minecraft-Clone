using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldData
{
    public static int WorldSize = 64000;
    public static int ChunkSize = 32;
    public static int NumOfChunks = WorldSize / ChunkSize;

    public static int MinHeight = 1;
    public static int MaxHeight = 64;

    public static int MapToWorldScaleFactor = 1;

    public static HashSet<Vector2Int> ActiveChunkSet;

    public static Vector3 PlayerPosition;

    static WorldData()
    {
        Evaluate();
    }

    public static void Evaluate()
    {
        NumOfChunks = WorldSize / ChunkSize;
        
        ActiveChunkSet = new HashSet<Vector2Int>();
        PlayerPosition = Vector3.zero;
        //Debug.Log("Number of Chunks: " + NumOfChunks);
    }
}
