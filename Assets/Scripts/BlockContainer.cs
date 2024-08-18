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

    // Merges all Same ID Blocks into a Single Mesh
    // Call this function when all the Blocks are Positioned in the World Correctly
    public Mesh MergeIntoSingleMesh(BlockID blockID)
    {
        int NumOfBlocks = 0;
        for (int i = 0; i < BlockIDList.Count; ++i)
        {
            if (BlockIDList[i] == blockID)
            {
                ++NumOfBlocks;
            }
        }
        CombineInstance[] CombineBlockList = new CombineInstance[NumOfBlocks];
        int j = 0;
        for (int i = 0; i < BlockList.Count; ++i)
        {
            if (BlockIDList[i] != blockID)
                continue;

            CombineBlockList[j].mesh = BlockList[i].GetComponent<MeshFilter>().sharedMesh;
            CombineBlockList[j].transform = BlockList[i].GetComponent<MeshFilter>().transform.localToWorldMatrix;
            ++j;
        }
        
        Mesh MeshRef = new Mesh();
        MeshRef.CombineMeshes(CombineBlockList);
        return MeshRef;
    }
}
