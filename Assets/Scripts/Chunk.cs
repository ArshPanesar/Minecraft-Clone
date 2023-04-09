using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public bool IsActive;

    private int[,] HeightMap;

    private Vector2Int Position;
    private BlockContainer BlockContainer;

    public Chunk()
    {
        IsActive = false;
        HeightMap = new int[WorldData.ChunkSize, WorldData.ChunkSize];
        
        Position = new Vector2Int(0, 0);
        BlockContainer = new BlockContainer();
    }

    public void Generate(Vector2Int StartPosition, NoiseGenerator.NoiseParameters NoiseParam)
    {
        IsActive = true;
        Position = StartPosition;

        Vector2Int EndPosition = StartPosition + (new Vector2Int(WorldData.ChunkSize, WorldData.ChunkSize));
        
        int hx = 0, hy = 0;
        for (int i = StartPosition.y; i < EndPosition.y; ++i)
        {
            for (int j = StartPosition.x; j < EndPosition.x; ++j)
            {
                float y = (float)i / (float)100 * NoiseParam.NoiseScale;
                float x = (float)j / (float)100 * NoiseParam.NoiseScale;

                Vector3 Shift = Vector3.zero;//WorldData.PlayerPosition;
                float noise = NoiseGenerator.ImprovedPerlinNoise(x + Shift.x, y + Shift.z, 0.0f);
                noise = Mathf.Clamp(noise, -1.0f, 1.0f);

                // Scaling Height from [-1, 1] Range
                int height = (int)(((float)WorldData.MaxHeight - (float)WorldData.MinHeight) * ((noise + 1.0f) / (1.0f + 1.0f)) + (float)WorldData.MinHeight);

                //Debug.Log("hx, hy = " + hx + ", " + hy);
                //Debug.Log("Size = " + HeightMap.Length + ", " + HeightMap.LongLength);

                HeightMap[hx, hy] = height;
                ++hx;
            }

            hx = 0;
            ++hy;
        }
    }

    public void PlaceBlocks(Grid UnityGrid)
    {
        // Generate the Actual Blocks
        List<Vector3Int> high_cell_list = new List<Vector3Int>();
        for (int i = 0; i < WorldData.ChunkSize; i++)
        {
            for (int j = 0; j < WorldData.ChunkSize; j++)
            {
                int x = Position.x + j * WorldData.MapToWorldScaleFactor;
                int y = HeightMap[j, i] * WorldData.MapToWorldScaleFactor;
                int z = Position.y + i * WorldData.MapToWorldScaleFactor;

                for (int p = 0; p < WorldData.MapToWorldScaleFactor; ++p)
                {
                    for (int k = 0; k < WorldData.MapToWorldScaleFactor; ++k)
                    {
                        var block = BlockContainer.CreateBlock();

                        block.transform.position = UnityGrid.CellToWorld(new Vector3Int(x + p, y, z + k));
                        if (HeightMap[j, i] > WorldData.MinHeight)
                        {
                            high_cell_list.Add(new Vector3Int(x + p, y, z + k));
                        }
                    }
                }
            }
        }
    }

    public void Destroy()
    {
        BlockContainer.ClearAll();
        IsActive = false;
    }
}
