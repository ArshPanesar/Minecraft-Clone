using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// For OBJLoader
using Dummiesman;

public class BlockContainer
{
    public enum BlockID
    {
        GRASS = 0,
        DIRT
    };

    public class CreateBlocksTask : UnityMainThreadManager.Task
    {
        public int in_NumOfBlocks = 0;
        public BlockID in_BlockID = BlockID.GRASS;
        public BlockContainer inout_BlockContainerRef;

        public override void Execute()
        {
            for (int i = 0; i < in_NumOfBlocks; ++i)
            {
                inout_BlockContainerRef.CreateBlock(in_BlockID);
            }
        }
    }

    public class PlaceBlocksTask : UnityMainThreadManager.Task
    {
        public BlockContainer inout_BlockContainerRef;
        public List<Vector3Int> in_PosList;
        public Grid in_UnityGrid;
        public int in_StartIndex;
        public int in_NumOfBlocks;

        public override void Execute()
        {
            int j = 0;
            for (int i = in_StartIndex; i < (in_StartIndex + in_NumOfBlocks); ++i)
            {
                var Block = inout_BlockContainerRef.BlockList[i];
                Block.transform.position = in_UnityGrid.CellToWorld(in_PosList[j++]);
            }
        }
    }

    public List<GameObject> BlockList;
    public int BlockIndex = 0;

    public static GameObject GrassBlock;
    public static GameObject DirtBlock;

    public HashSet<GameObject> VisibleBlocks;

    public BlockContainer()
    {
        BlockList = new List<GameObject>();

        VisibleBlocks = new HashSet<GameObject>();
    }

    public static void LoadModels()
    {
        GrassBlock = new OBJLoader().Load("Assets/Resources/Models/GrassBlock.obj");
        DirtBlock = new OBJLoader().Load("Assets/Resources/Models/DirtBlock.obj");
        GrassBlock = GrassBlock.transform.GetChild(0).gameObject;
        DirtBlock = DirtBlock.transform.GetChild(0).gameObject;


        // Set Up the GrassBlock
        //
        // Block will never move
        GrassBlock.isStatic = true;
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
    }

    public GameObject CreateBlock(BlockID blockID = BlockID.GRASS)
    {
        switch (blockID)
        {
            case BlockID.DIRT:
                BlockList.Add(GameObject.Instantiate(DirtBlock));
                break;
            case BlockID.GRASS:
                BlockList.Add(GameObject.Instantiate(GrassBlock));
                break;
        }

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
        BlockList.Clear();
    }

    public void GenerateRenderBatches()
    {
        GameObject[] gameObjects = new GameObject[BlockList.Count - 1];
        for (int i = 1; i < BlockList.Count; ++i)
        {
            gameObjects[i - 1] = BlockList[i];
        }
        
        StaticBatchingUtility.Combine(gameObjects, BlockList[0]);
    }

    public void PrintVisible()
    {
        Debug.Log(VisibleBlocks.Count);
    }
}
