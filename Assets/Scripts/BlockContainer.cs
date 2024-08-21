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

    //public void MergeIntoBigMeshes(out List<Mesh> MeshList)
    //{
    //    // BELOW CODE WILL BE MOVED INTO TASKS
    //    const int MaxVerticesAllowed = 65536;

    //    // Calculate Number of Big Meshes
    //    int VerticesPerBlock = BlockList[0].GetComponent<MeshFilter>().sharedMesh.vertexCount;
    //    int NumOfVertices = BlockList.Count * VerticesPerBlock;
    //    int NumOfBigMeshes = Mathf.CeilToInt((float)NumOfVertices / (float)MaxVerticesAllowed); // Dividing by Max Vertices Allowed Per Mesh

    //    // Generate Merged Meshes
    //    MeshList = new List<Mesh>(NumOfBigMeshes);
    //    int NumOfBlocksPerMesh = Mathf.FloorToInt((float)MaxVerticesAllowed / (float)VerticesPerBlock);
    //    int StartBlockIndex = 0;
    //    for (int i = 0; i < NumOfBigMeshes; ++i)
    //    {
    //        int EndBlockIndex = Mathf.Clamp(StartBlockIndex + NumOfBlocksPerMesh, 0, BlockList.Count);

    //        CombineInstance[] CombineBlockList = new CombineInstance[EndBlockIndex - StartBlockIndex];
    //        int k = 0;
    //        for (int j = StartBlockIndex; j < EndBlockIndex; ++j)
    //        {
    //            CombineBlockList[k].mesh = BlockList[j].GetComponent<MeshFilter>().sharedMesh;
    //            CombineBlockList[k].transform = BlockList[j].GetComponent<MeshFilter>().transform.localToWorldMatrix;
    //            ++k;
    //        }
    //        StartBlockIndex = EndBlockIndex;

    //        Mesh MeshRef = new Mesh();
    //        MeshRef.CombineMeshes(CombineBlockList, true);
    //        MeshList.Add(MeshRef);
    //    }
    //}

    public class GenerateMergedMeshesTask : TaskManager.Task
    {
        public int in_MaxMeshesCombine = 128;

        public List<GameObject> in_BlockList;
        
        public List<Mesh> out_MeshList;

        private bool PerformedCalc = false;
        private bool AllMerged = false;
        private bool CreatedPartialTasks = false;

        private int VerticesPerBlock;
        private int NumOfVertices;
        private int NumOfBigMeshes;
        private int NumOfBlocksPerMesh;
        private int CurrentMeshListCount;

        private List<GeneratePartialMergedMeshesTask> PartialTasksList;

        public override void Execute()
        {
            WaitingFlag = true;

            if (!PerformedCalc)
            {
                const int MaxVerticesAllowed = 65536;

                // Calculate Number of Big Meshes
                VerticesPerBlock = in_BlockList[0].GetComponent<MeshFilter>().sharedMesh.vertexCount;
                NumOfVertices = in_BlockList.Count * VerticesPerBlock;
                NumOfBigMeshes = Mathf.CeilToInt((float)NumOfVertices / (float)MaxVerticesAllowed); // Dividing by Max Vertices Allowed Per Mesh

                // Generate Merged Meshes
                NumOfBlocksPerMesh = Mathf.FloorToInt((float)MaxVerticesAllowed / (float)VerticesPerBlock);

                PerformedCalc = true;
            }

            if (!AllMerged)
            {
                if (!CreatedPartialTasks)
                {
                    // Create the Tasks
                    CurrentMeshListCount = (int)((float)in_BlockList.Count / (float)in_MaxMeshesCombine) + (in_BlockList.Count % in_MaxMeshesCombine);
                    
                    PartialTasksList = new List<GeneratePartialMergedMeshesTask>(CurrentMeshListCount);
                    int StartBlockIndex = 0;
                    int EndBlockIndex = in_MaxMeshesCombine;
                    for (int i = 0; i < CurrentMeshListCount; ++i)
                    {
                        GeneratePartialMergedMeshesTask NewTask = new GeneratePartialMergedMeshesTask();
                        NewTask.in_BlockList = in_BlockList;
                        NewTask.in_StartBlockIndex = StartBlockIndex;
                        NewTask.in_EndBlockIndex = EndBlockIndex;

                        TaskManager.GetInstance().Enqueue(NewTask);
                        PartialTasksList.Add(NewTask);
                        
                        StartBlockIndex = EndBlockIndex;
                        EndBlockIndex += in_MaxMeshesCombine;
                        EndBlockIndex = (EndBlockIndex > in_BlockList.Count) ? in_BlockList.Count : EndBlockIndex;
                    }

                    CreatedPartialTasks = true;
                }

                // Complete the Tasks
                for (int i = 0; i < CurrentMeshListCount; ++i)
                {
                    if (!PartialTasksList[i].Completed)
                    {
                        return;
                    }
                }

                // All Done!
                out_MeshList = new List<Mesh>(CurrentMeshListCount);
                for (int i = 0; i < CurrentMeshListCount; ++i)
                {
                    out_MeshList.Add(PartialTasksList[i].out_Mesh);
                }

                AllMerged = true;
            } 

            WaitingFlag = false;
        }
    }

    class GeneratePartialMergedMeshesTask : TaskManager.Task
    {
        public List<GameObject> in_BlockList;
        public int in_StartBlockIndex;
        public int in_EndBlockIndex;

        public Mesh out_Mesh;
        
        public override void Execute()
        {
            CombineInstance[] CombineBlockList = new CombineInstance[in_EndBlockIndex - in_StartBlockIndex];            
            int k = 0;
            for (int j = in_StartBlockIndex; j < in_EndBlockIndex; ++j)
            {
                CombineBlockList[k].mesh = in_BlockList[j].GetComponent<MeshFilter>().sharedMesh;
                CombineBlockList[k].transform = in_BlockList[j].GetComponent<MeshFilter>().transform.localToWorldMatrix;
                ++k;
            }

            out_Mesh = new Mesh();
            out_Mesh.CombineMeshes(CombineBlockList, true);
        }
    }
}