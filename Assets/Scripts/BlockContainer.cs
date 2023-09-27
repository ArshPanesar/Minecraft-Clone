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

    static public GameObject GrassBlock;
    static public GameObject DirtBlock;

    public BlockContainer()
    {
        BlockList = new List<GameObject>(WorldData.ChunkSize * WorldData.ChunkSize);
        /*GrassBlock = new OBJLoader().Load("Assets/Resources/Models/GrassBlock.obj");
        DirtBlock = new OBJLoader().Load("Assets/Resources/Models/DirtBlock.obj");
        GrassBlock = GrassBlock.gameObject.transform.GetChild(0).gameObject;
        DirtBlock = DirtBlock.gameObject.transform.GetChild(0).gameObject;*/
        
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
        for (int i = 0; i < BlockList.Count; ++i)
        {
            GameObject.Destroy(BlockList[i]);
        }
        BlockList.Clear();
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
