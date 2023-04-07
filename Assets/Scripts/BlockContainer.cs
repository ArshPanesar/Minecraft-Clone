using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// For OBJLoader
using Dummiesman;

public class BlockContainer
{
    public List<GameObject> BlockList;
    //public int BlockCount = 1000000;
    public int BlockIndex = 0;

    public GameObject grass_block;

    public BlockContainer()
    {
        BlockList = new List<GameObject>();
        grass_block = new OBJLoader().Load("C:/Users/Arsh Panesar/Desktop/Redo/PGW/Minecraft/Assets/Resources/Models/GrassBlock.obj");
    
        // Set Up the GrassBlock
        var material = grass_block.GetComponentInChildren<MeshRenderer>().material;
        material.enableInstancing = true;
    }

    public GameObject CreateBlock()
    {
        OBJLoader loader = new OBJLoader();
        BlockList.Add(GameObject.Instantiate(grass_block));
        
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
}
