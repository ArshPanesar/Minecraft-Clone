using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockContainer
{
    public List<GameObject> BlockList;
    public List<BlockID> BlockIDList;
    public List<int> BlockPoolIndexList;
    
    public BlockContainer()
    {
        BlockList = new List<GameObject>(WorldData.ChunkSize * WorldData.ChunkSize);
        BlockIDList = new List<BlockID>(WorldData.ChunkSize * WorldData.ChunkSize);
        BlockPoolIndexList = new List<int>(WorldData.ChunkSize * WorldData.ChunkSize);
    }

    public GameObject CreateBlock(BlockID blockID = BlockID.GRASS)
    {
        int UsedID = 0;
        BlockList.Add(BlockPool.GetInstance().CreateBlock(out UsedID, blockID));
        BlockIDList.Add(blockID);
        BlockPoolIndexList.Add(UsedID);

        return BlockList[BlockList.Count - 1];
    }

    public GameObject GetBlock(int index)
    {
        if (index < 0 || index >= BlockList.Count)
            return null;
        return BlockList[index];
    }

    public void ClearAll()
    {
        for (int i = 0; i < BlockList.Count; ++i)
        {
            BlockPool.GetInstance().DestroyBlock(BlockPoolIndexList[i], BlockIDList[i]);
        }
        BlockList.Clear();
        BlockIDList.Clear();
        BlockPoolIndexList.Clear();
    }

    public void GenerateRenderBatches()
    {
        //GameObject[] gameObjects = new GameObject[BlockList.Count - 1];
        //for (int i = 1; i < BlockList.Count; ++i)
        //{
        //    gameObjects[i - 1] = BlockList[i];
        //}

        //StaticBatchingUtility.Combine(gameObjects, BlockList[0]);
    }
}
