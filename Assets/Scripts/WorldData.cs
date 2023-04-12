using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;

public class WorldData
{
    public static int WorldSize = 64000;
    public static int ChunkSize = 32;
    public static int WorldSmoothingFactor = 200;
    public static int NumOfChunks = WorldSize / ChunkSize;

    public static int MinHeight = 1;
    public static int MaxHeight = 64;

    public static int MapToWorldScaleFactor = 1;

    public static HashSet<Vector2Int> ActiveChunkSet;

    public static Vector3 PlayerPosition;

    public class ManagedChunkData
    {
        public static ConcurrentDictionary<int, BlockContainer> BlockContainerDict;
        public static ConcurrentDictionary<int, int[,]> HeightMapDict;

        private static ConcurrentQueue<int> IndexQueue;
        
        private static bool Initialized = false;

        public static void Reset()
        {
            BlockContainerDict = new ConcurrentDictionary<int, BlockContainer>();
            HeightMapDict = new ConcurrentDictionary<int, int[,]>();

            IndexQueue = new ConcurrentQueue<int>();
            for (int i = 0; i < 512; ++i)
            {
                IndexQueue.Enqueue(i);
            }
        }

        public static int CreateData()
        {
            int Index = -1;

            // Keep Trying to get an Index
            while (!IndexQueue.TryDequeue(out Index)) { };

            // Add Data
            BlockContainer NewBlockContainer = new BlockContainer();
            int[,] NewHeightMap = new int[ChunkSize, ChunkSize];

            while (!BlockContainerDict.TryAdd(Index, NewBlockContainer)) { };
            while (!HeightMapDict.TryAdd(Index, NewHeightMap)) { };

            return Index;
        }

        public static void DestroyData(int Index)
        {
            // Remove Data
            BlockContainer OldBlockContainer;
            int[,] OldHeightMap;

            while (!BlockContainerDict.TryRemove(Index, out OldBlockContainer)) { };
            while (!HeightMapDict.TryRemove(Index, out OldHeightMap)) { };

            IndexQueue.Enqueue(Index);
        }
    }

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

        ManagedChunkData.Reset();
    }
}
