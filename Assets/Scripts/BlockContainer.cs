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

    public List<GameObject> BlockList;
    //public int BlockCount = 1000000;
    public int BlockIndex = 0;

    public GameObject grass_block;
    public GameObject dirt_block;

    public HashSet<GameObject> visibleBlocks;

    public BlockContainer()
    {
        BlockList = new List<GameObject>();
        grass_block = new OBJLoader().Load("C:/Users/Arsh Panesar/Desktop/Redo/PGW/Minecraft/Assets/Resources/Models/GrassBlock.obj");
        dirt_block = new OBJLoader().Load("Assets/Resources/Models/DirtBlock.obj");
        grass_block = grass_block.transform.GetChild(0).gameObject;
        dirt_block = dirt_block.transform.GetChild(0).gameObject;

        visibleBlocks = new HashSet<GameObject>();

        // Set Up the GrassBlock
        //
        // Block will never move
        grass_block.isStatic = true;
        // Instance all materials in every block
        var materials = grass_block.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        foreach (var m in materials) 
        {
            m.enableInstancing = true;
        }
        materials = dirt_block.GetComponentInChildren<MeshRenderer>().sharedMaterials;
        foreach (var m in materials)
        {
            m.enableInstancing = true;
        }

        // Attach Visibility Notifier
        grass_block.AddComponent<VisibleBlocksTracker>();
        grass_block.GetComponent<VisibleBlocksTracker>().visibleGameObjects = visibleBlocks;
    }

    public GameObject CreateBlock(BlockID blockID = BlockID.GRASS)
    {
        OBJLoader loader = new OBJLoader();
        switch (blockID) 
        {
            case BlockID.DIRT:
                BlockList.Add(GameObject.Instantiate(dirt_block));
                break;
            case BlockID.GRASS:
                BlockList.Add(GameObject.Instantiate(grass_block));
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
        Debug.Log(visibleBlocks.Count);
    }
}