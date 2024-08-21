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
    public void MergeIntoBigMeshes(out List<Mesh> MeshList)
    {
        const int MaxVerticesAllowed = 65536;

        // Calculate Number of Big Meshes
        int VerticesPerBlock = BlockList[0].GetComponent<MeshFilter>().sharedMesh.vertexCount;
        int NumOfVertices = BlockList.Count * VerticesPerBlock;
        int NumOfBigMeshes = Mathf.CeilToInt((float)NumOfVertices / (float)MaxVerticesAllowed); // Dividing by Max Vertices Allowed Per Mesh

        // Generate Merged Meshes
        MeshList = new List<Mesh>(NumOfBigMeshes);
        int NumOfBlocksPerMesh = Mathf.FloorToInt((float)MaxVerticesAllowed / (float)VerticesPerBlock);
        int StartBlockIndex = 0;
        for (int i = 0; i < NumOfBigMeshes; ++i)
        {
            int EndBlockIndex = Mathf.Clamp(StartBlockIndex + NumOfBlocksPerMesh, 0, BlockList.Count);
            
            CombineInstance[] CombineBlockList = new CombineInstance[EndBlockIndex - StartBlockIndex];
            int k = 0;
            for (int j = StartBlockIndex; j < EndBlockIndex; ++j)
            {
                CombineBlockList[k].mesh = BlockList[j].GetComponent<MeshFilter>().sharedMesh;
                CombineBlockList[k].transform = BlockList[j].GetComponent<MeshFilter>().transform.localToWorldMatrix;
                ++k;
            }
            StartBlockIndex = EndBlockIndex;

            Mesh MeshRef = new Mesh();
            MeshRef.CombineMeshes(CombineBlockList, true);
            MeshList.Add(MeshRef);
        }
    }
}
