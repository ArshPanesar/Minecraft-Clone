using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    // Terrain Map Parameters
    public int GridWidth = 100;
    public int GridHeight = 100;

    public int[,] Grid;

    // Noise Function Parameters
    public float NoiseScale = 3.5f;
    public float Octaves = 6;
    public float Amplitude = 0.5f;
    public float Freq = 1.0f;
    public float FreqGain = 2.0f;
    public float AmplitudeGain = 0.5f;
    public float PerlinShift = 0.0f;
    public float PerlinShiftGain = 2.0f;

    // Procedural Generation Factors
    public int MaxHeight = 64;
    public int MinHeight = 1;
    public float LandFilter01 = 0.5f; // Noise greater than this value will be Land

    
    // World Objects
    BlockContainer block_container;
    public int MapToWorldScaleFactor = 1;

    // Attach this script to a sprite to visualize the terrain map
    private Texture2D tex;
    private SpriteRenderer spr_renderer_ref;
    private Sprite spr;
    private Grid u_grid;

    TerrainGenerator()
    {
        Grid = new int[GridWidth, GridHeight];

        int Val = 0;
        for (int i = 0; i < GridWidth; i++)
        {
            for (int j = 0; j < GridHeight; ++j)
            {
                Grid[i, j] = Val;
            }
            ++Val;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Grid = new int[GridWidth, GridHeight];

        int Val = 0;
        for (int i = 0; i < GridWidth; i++)
        {
            for (int j = 0; j < GridHeight; ++j)
            {
                Grid[i, j] = Val;
            }
            ++Val;
        }

        block_container = new BlockContainer();
        //var block = block_container.CreateBlock();
        //block.transform.position = Vector3.zero;
        
        spr_renderer_ref = GetComponent<SpriteRenderer>();
        u_grid = GetComponent<Grid>();

        GenerateTerrainMap();
        //DisplayTerrainMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GenerateTerrainMap();
            //DisplayTerrainMap();
        }
    }

    public void GenerateTerrainMap()
    {
        block_container.ClearAll();

        for (int i = 0; i < GridHeight; i++)
        {
            for (int j = 0; j < GridWidth; j++)
            {
                float y = (float)i / (float)GridHeight * NoiseScale;
                float x = (float)j / (float)GridWidth * NoiseScale;

                float noise = FractalBrownianMotion(x, y);
                noise = Mathf.Clamp01(noise);

                //if (noise < LandFilter01)
                //{
                //    Grid[j, i] = MinHeight;
                //    continue;
                //    //noise = (noise - LandFilter01) * 0.5f + LandFilter01;
                //}
                if (noise < 0.1f)
                {
                    Grid[j, i] = MinHeight;
                }
                else if (noise < 0.2f)
                {
                    Grid[j, i] = MinHeight + 1;
                }
                else if (noise < 0.3f)
                {
                    Grid[j, i] = MinHeight + 2;
                }
                else
                {
                    //LandFilter01 = 0.0f;
                    int height = (int)(((float)MaxHeight - (float)MinHeight) * ((noise - 0.3f) / (1.0f - 0.3f)) + (float)MinHeight);
                    if (height < MinHeight + 3)
                    {
                        height = MinHeight + 3;
                    }

                    Grid[j, i] = height;
                }
                //Debug.Log(noise);
            }
        }

        // Generate the Actual World
        List<Vector3Int> cell_list = new List<Vector3Int>();
        for (int i = 0; i < GridHeight; i++)
        {
            for (int j = 0; j < GridWidth; j++)
            {
                int x = j * MapToWorldScaleFactor;
                int y = Grid[j,i] * MapToWorldScaleFactor;
                int z = i * MapToWorldScaleFactor;

                for (int p = 0; p < MapToWorldScaleFactor; ++p)
                {
                    for (int k = 0; k < MapToWorldScaleFactor; ++k)
                    {
                        var block = block_container.CreateBlock();

                        block.transform.position = u_grid.CellToWorld(new Vector3Int(x + p, y, z + k));
                        if (Grid[j, i] > MinHeight)
                        {
                            cell_list.Add(new Vector3Int(x + p, y, z + k));
                        }
                    }
                }
            }
        }

        // Fill Blocks till Min Height
        for (int i = 0; i < cell_list.Count; i++)
        {
            var c = cell_list[i];

            for (int j = c.y - 4; j < c.y; j++)
            {
                var block = block_container.CreateBlock();
                block.transform.position = u_grid.CellToWorld(new Vector3Int(c.x, j, c.z));
            }
        }
    }

    void DisplayTerrainMap()
    {
        if (spr_renderer_ref == null)
        {
            Debug.Log("DisplayTerrainMap() Warn: No Sprite Renderer Found.");
            return;
        }

        tex = new Texture2D(GridWidth, GridHeight);

        for (int i = 0; i < tex.height; ++i)
        {
            for (int j = 0; j < tex.width; ++j)
            {
                //TerrainTile tile = Grid.GetCell(j, i);

                //if (tile.TileID == TerrainTile.eTileID.LAND)
                {
                    tex.SetPixel(j, i, Color.green);
                }
                //else
                {
                    tex.SetPixel(j, i, Color.blue);
                }
            }
        }

        tex.Apply();

        // Generate the Sprite
        spr = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        spr_renderer_ref.sprite = spr;
    }

    // NOISE FUNCTIONS
    private float FractalBrownianMotion(float x, float y)
    {
        float val = 0.0f;
        float amplitude = Amplitude;
        float freq = Freq;
        float freq_gain = FreqGain;
        float amp_gain = AmplitudeGain;
        float shift = PerlinShift;
        float shift_gain = PerlinShiftGain;
        for (int i = 0; i < Octaves; ++i)
        {
            val += amplitude * Mathf.PerlinNoise(shift + x * freq, shift + y * freq);
            freq *= freq_gain;
            amplitude *= amp_gain;
            shift *= shift_gain;
        }

        return val;
    }
}
