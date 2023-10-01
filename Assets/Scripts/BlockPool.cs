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
        public Queue<int> FreeIDs;
        public Queue<int> UsedIDs;
        public int PoolStartIndex = -1;

        public BlockList()
        {
            FreeIDs = new Queue<int>();
            UsedIDs = new Queue<int>();
        }
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

    public int NumOfBlocks = 60000;

    private List<GameObject> PooledBlocks;

    private List<BlockList> BlockListTable;
    
    public BlockPool()
    {
        // Set Up the Blocks
        //
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
        PooledBlocks = new List<GameObject>();

        BlockListTable = new List<BlockList>();
        for (int i = 0; i < (int)BlockID.NUM_OF_BLOCKS; i++)
            BlockListTable.Add(new BlockList());

        // Assign Indices and Current
        BlockListTable[(int)BlockID.GRASS].PoolStartIndex = 0;
        BlockListTable[(int)BlockID.DIRT].PoolStartIndex = BlockListTable[(int)BlockID.GRASS].PoolStartIndex + NumOfBlocks;

        // Instantiate Each Block
        int IDOffset = 0;
        for (int i = 0; i < NumOfBlocks; i++)
        {
            PooledBlocks.Add(GameObject.Instantiate(GrassBlock));
            BlockListTable[(int)BlockID.GRASS].FreeIDs.Enqueue(IDOffset + i);
        }
        IDOffset = PooledBlocks.Count;
        for (int i = 0; i < NumOfBlocks; i++)
        {
            PooledBlocks.Add(GameObject.Instantiate(DirtBlock));
            BlockListTable[(int)BlockID.DIRT].FreeIDs.Enqueue(IDOffset + i);
        }
    }

    public GameObject CreateBlock(BlockID blockID = BlockID.GRASS)
    {
        int ID = -1;
        switch (blockID)
        {
            case BlockID.DIRT:
                ID = BlockListTable[(int)BlockID.DIRT].FreeIDs.Dequeue();
                BlockListTable[(int)BlockID.DIRT].UsedIDs.Enqueue(ID);

                //Debug.Log("Dirt Blocks: " + BlockListTable[(int)BlockID.DIRT].UsedIDs.Count);


                return PooledBlocks[ID];

            case BlockID.GRASS:
                ID = BlockListTable[(int)BlockID.GRASS].FreeIDs.Dequeue();
                BlockListTable[(int)BlockID.GRASS].UsedIDs.Enqueue(ID);
                
                //Debug.Log("Grass Blocks: " + BlockListTable[(int)BlockID.GRASS].UsedIDs.Count);
                
                return PooledBlocks[ID];
        }

        return null;
    }

    public void DestroyBlock(GameObject Block, BlockID BlockID)
    {
        int ID = -1;
        switch (BlockID)
        {
            case BlockID.DIRT:
                ID = BlockListTable[(int)BlockID.DIRT].UsedIDs.Dequeue();
                BlockListTable[(int)BlockID.DIRT].FreeIDs.Enqueue(ID);

                //Debug.Log("Dirt Blocks: " + BlockListTable[(int)BlockID.DIRT].UsedIDs.Count);

                break;

            case BlockID.GRASS:
                ID = BlockListTable[(int)BlockID.GRASS].UsedIDs.Dequeue();
                BlockListTable[(int)BlockID.GRASS].FreeIDs.Enqueue(ID);

                //Debug.Log("Grass Blocks: " + BlockListTable[(int)BlockID.GRASS].UsedIDs.Count);

                break; 
        }
    }

    public void ResetAllBlocks()
    {
        for (int i = 0; i < PooledBlocks.Count; ++i)
            PooledBlocks[i].transform.position = Vector3.zero;
    }
}
