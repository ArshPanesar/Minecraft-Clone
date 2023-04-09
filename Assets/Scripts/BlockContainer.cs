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
    public int BlockIndex = 0;

    public GameObject GrassBlock;
    public GameObject DirtBlock;

    public HashSet<GameObject> VisibleBlocks;

    public BlockContainer()
    {
        BlockList = new List<GameObject>();
        GrassBlock = new OBJLoader().Load("Assets/Resources/Models/GrassBlock.obj");
        DirtBlock = new OBJLoader().Load("Assets/Resources/Models/DirtBlock.obj");
        GrassBlock = GrassBlock.transform.GetChild(0).gameObject;
        DirtBlock = DirtBlock.transform.GetChild(0).gameObject;

        VisibleBlocks = new HashSet<GameObject>();

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

        // Attach Visibility Notifier
        GrassBlock.AddComponent<VisibleBlocksTracker>();
        GrassBlock.GetComponent<VisibleBlocksTracker>().visibleGameObjects = VisibleBlocks;
    }

    public GameObject CreateBlock(BlockID blockID = BlockID.GRASS)
    {
        OBJLoader loader = new OBJLoader();
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
