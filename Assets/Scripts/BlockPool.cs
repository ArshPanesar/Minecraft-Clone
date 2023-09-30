using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BlockID
{
    GRASS = 0,
    DIRT
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

    public int NumOfBlocks = 65536;

    private List<GameObject> PooledBlocks;

    private BlockList GrassBlockList;
    private BlockList DirtBlockList;

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

        GrassBlockList = new BlockList();
        DirtBlockList = new BlockList();

        // Assign Indices and Current
        GrassBlockList.PoolStartIndex = 0;
        DirtBlockList.PoolStartIndex = GrassBlockList.PoolStartIndex + NumOfBlocks;

        // Instantiate Each Block
        int IDOffset = 0;
        for (int i = 0; i < NumOfBlocks; i++)
        {
            PooledBlocks.Add(GameObject.Instantiate(GrassBlock));
            GrassBlockList.FreeIDs.Enqueue(IDOffset + i);
        }
        IDOffset = PooledBlocks.Count;
        for (int i = 0; i < NumOfBlocks; i++)
        {
            PooledBlocks.Add(GameObject.Instantiate(DirtBlock));
            DirtBlockList.FreeIDs.Enqueue(IDOffset + i);
        }
    }

    public GameObject CreateBlock(BlockID blockID = BlockID.GRASS)
    {
        int ID = -1;
        switch (blockID)
        {
            case BlockID.DIRT:
                ID = DirtBlockList.FreeIDs.Dequeue();
                DirtBlockList.UsedIDs.Enqueue(ID);

                return PooledBlocks[ID];

            case BlockID.GRASS:
                ID = GrassBlockList.FreeIDs.Dequeue();
                GrassBlockList.UsedIDs.Enqueue(ID);

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
                ID = DirtBlockList.UsedIDs.Dequeue();
                DirtBlockList.FreeIDs.Enqueue(ID);

                break;

            case BlockID.GRASS:
                ID = GrassBlockList.UsedIDs.Dequeue();
                GrassBlockList.FreeIDs.Enqueue(ID);

                break; 
        }
    }
}
