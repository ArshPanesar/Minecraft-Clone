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
        
        // TEMP
        float max = -1.0f, min = 1.0f;
        int hmax = WorldData.MinHeight, hmin = WorldData.MaxHeight;
        
        for (int i = StartPosition.y; i < EndPosition.y; ++i)
        {
            for (int j = StartPosition.x; j < EndPosition.x; ++j)
            {
                float y = (float)i / (float)WorldData.WorldSmoothingFactor * NoiseParam.NoiseScale;
                float x = (float)j / (float)WorldData.WorldSmoothingFactor * NoiseParam.NoiseScale;

                float noise = NoiseGenerator.FractalBrownianMotion(x, y, NoiseParam); //NoiseGenerator.ImprovedPerlinNoise(x, y, 0.0f);
                max = Mathf.Max(max, noise);
                min = Mathf.Min(min, noise);

                noise = Mathf.Clamp(noise, -0.5f, 0.5f);

                // Scaling Height from [-1, 1] Range
                int height = (int)( ( (noise + 0.5f) / (0.5f - (-0.5f)) ) * ((float)WorldData.MaxHeight - (float)WorldData.MinHeight) + (float)WorldData.MinHeight );
                hmax = Mathf.Max(hmax, height);
                hmin = Mathf.Min(hmin, height);

                /*if (height < 20)
                {
                    height = 20;
                }
                else if (height < 25)
                {
                    height = 21;
                }
                else if (height < 30)
                {
                    height = 22;
                }
                else
                {
                    height -= 7;
                }*/

                //int height = (int)(((float)WorldData.MaxHeight - (float)WorldData.MinHeight) * ((noise - 1.0f) / (1.0f - 0.0f)) + (float)WorldData.MinHeight);
                //int height = 3;
                HeightMap[hx, hy] = height;
                ++hx;
            }

            hx = 0;
            ++hy;
        }

        Debug.Log("Min, Max = " + hmin + ", " + hmax);
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
