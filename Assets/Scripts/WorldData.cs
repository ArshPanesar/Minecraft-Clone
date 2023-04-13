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
        public static Dictionary<int, BlockContainer> BlockContainerDict;
        public static Dictionary<int, int[,]> HeightMapDict;
        
        private static Queue<int> IndexQueue;
        
        public static void Reset()
        {
            BlockContainerDict = new Dictionary<int, BlockContainer>();
            HeightMapDict = new Dictionary<int, int[,]>();
            
            IndexQueue = new Queue<int>();
            for (int i = 0; i < 512; ++i)
            {
                IndexQueue.Enqueue(i);
            }
        }

        public static int CreateData()
        {
            int Index = -1;

            lock (IndexQueue)
            {
                Index = IndexQueue.Dequeue();
                
                // Add Data
                BlockContainer NewBlockContainer = new BlockContainer();
                int[,] NewHeightMap = new int[ChunkSize, ChunkSize];

                lock (BlockContainerDict) { BlockContainerDict.Add(Index, NewBlockContainer); }
                lock (HeightMapDict) { HeightMapDict.Add(Index, NewHeightMap); }
                
                return Index;
            }
        }

        public static void DestroyData(int Index)
        {
            lock (BlockContainerDict) { BlockContainerDict.Remove(Index); }
            lock (HeightMapDict) { HeightMapDict.Remove(Index); }
            
            lock (IndexQueue) { IndexQueue.Enqueue(Index); }
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
        
        ManagedChunkData.Reset();
    }
}
