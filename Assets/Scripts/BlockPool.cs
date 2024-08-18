using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockID : int
{
    GRASS = 0,
    DIRT,
    NUM_OF_BLOCKS
};

public class BlockPool
{
    // Block Metadata Container for Fast Processing
    public class BlockList
    {
        public Queue<int> FreeIDs = new Queue<int>();
    }

    // Singleton
    private static BlockPool Instance = null;
    public static BlockPool GetInstance()
    {
        if (Instance == null)
        {
            Instance = new BlockPool();
        }
        return Instance;
    }

    public static GameObject GrassBlock;
    public static GameObject DirtBlock;

    public int NumOfBlocks = 50000;

    private List<GameObject>[] PooledBlocksTable;

    private List<BlockList> BlockListTable;
    
    public BlockPool()
    {
        // Set Up the Blocks
        //
        // Don't Render Blocks unless Created
        GrassBlock.GetComponent<MeshRenderer>().enabled = false;
        DirtBlock.GetComponent<MeshRenderer>().enabled = false;
        // Block will never move
        GrassBlock.isStatic = true;
        DirtBlock.isStatic = true;
        // Instance all materials in every block
        var materials = GrassBlock.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        foreach (var m in materials)
        {
            m.enableInstancing = true;
        }
        materials = DirtBlock.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        foreach (var m in materials)
        {
            m.enableInstancing = true;
        }

        // Prepare the Pool
        PooledBlocksTable = new List<GameObject>[(int)BlockID.NUM_OF_BLOCKS];
        for (int i = 0; i < PooledBlocksTable.Length; ++i)
            PooledBlocksTable[i] = new List<GameObject>();

        BlockListTable = new List<BlockList>();
        for (int i = 0; i < (int)BlockID.NUM_OF_BLOCKS; i++)
            BlockListTable.Add(new BlockList());

        // Generate Pool
        ExpandPool();
    }

    private void ExpandPool()
    {
        // Instantiate Each Block
        //
        int IDOffset = PooledBlocksTable[(int)BlockID.GRASS].Count;
        for (int i = 0; i < NumOfBlocks; i++)
        {
            PooledBlocksTable[(int)BlockID.GRASS].Add(GameObject.Instantiate(GrassBlock));
            BlockListTable[(int)BlockID.GRASS].FreeIDs.Enqueue(IDOffset + i);
        }
        IDOffset = PooledBlocksTable[(int)BlockID.DIRT].Count;
        for (int i = 0; i < NumOfBlocks; i++)
        {
            PooledBlocksTable[(int)BlockID.DIRT].Add(GameObject.Instantiate(DirtBlock));
            BlockListTable[(int)BlockID.DIRT].FreeIDs.Enqueue(IDOffset + i);
        }
    }

    public GameObject CreateBlock(out int UsedID, BlockID blockID = BlockID.GRASS)
    {
        // Check if More Blocks are needed
        if (BlockListTable[(int)blockID].FreeIDs.Count - 1 < 1)
        {
            ExpandPool();
        }

        UsedID = BlockListTable[(int)blockID].FreeIDs.Dequeue();
        GameObject BlockRef = PooledBlocksTable[(int)blockID][UsedID];
        
        BlockRef.GetComponent<MeshRenderer>().enabled = true;
        return BlockRef;
    }

    public void DestroyBlock(int UsedID, BlockID blockID)
    {
        GameObject BlockRef = PooledBlocksTable[(int)blockID][UsedID];
        BlockRef.transform.position = Vector3.zero;
        BlockRef.GetComponent<MeshRenderer>().enabled = false;
        BlockListTable[(int)blockID].FreeIDs.Enqueue(UsedID);
    }

    public void ResetAllBlocks()
    {
        for (int i = 0; i < PooledBlocksTable.Length; ++i)
        {
            for (int j = 0; j < PooledBlocksTable[i].Count; ++j)
                PooledBlocksTable[i][j].transform.position = Vector3.zero;
        }
    }
}
