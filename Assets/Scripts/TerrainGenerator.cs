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
        block_container.GenerateRenderBatches();
        //DisplayTerrainMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            GenerateTerrainMap();
            block_container.GenerateRenderBatches();
            //DisplayTerrainMap();
        }
        
        //block_container.PrintVisible();
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

                //float noise = FractalBrownianMotion(x, y);
                float noise = ImprovedPerlinNoise(x, y, 0.0f);
                //noise = Mathf.Clamp01(noise);
                noise = Mathf.Clamp(noise, -1.0f, 1.0f);
                /*if (noise < 0.1f)
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
                else*/
                {
                    int height = (int)(((float)MaxHeight - (float)MinHeight) * ((noise + 1.0f) / (1.0f + 1.0f)) + (float)MinHeight);
                    if (height < MinHeight + 3)
                    {
                        //height = MinHeight + 3;
                    }

                    Grid[j, i] = height;
                }
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
                //var block = block_container.CreateBlock(BlockContainer.BlockID.DIRT);
                //block.transform.position = u_grid.CellToWorld(new Vector3Int(c.x, j, c.z));
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
            val += amplitude * ImprovedPerlinNoise(shift + x * freq, shift + y * freq, 0.0f);//Mathf.PerlinNoise(shift + x * freq, shift + y * freq);
            freq *= freq_gain;
            amplitude *= amp_gain;
            //shift *= shift_gain;
        }

        return val;
    }

    // Improved Perlin Noise from https://mrl.cs.nyu.edu/~perlin/noise/
    //
    private int[] p = {    151,160,137,91,90,15,
                           131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
                           190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
                           88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
                           77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
                           102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
                           135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
                           5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
                           223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
                           129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
                           251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
                           49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
                           138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180,
                           151,160,137,91,90,15,
                           131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
                           190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
                           88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
                           77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
                           102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
                           135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
                           5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
                           223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
                           129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
                           251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
                           49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
                           138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
                  };

    public float ImprovedPerlinNoise(float x, float y, float z) // Output Range is [-1, 1]
    {
        int X = Mathf.FloorToInt(x) & 255,                  // FIND UNIT CUBE THAT
            Y = Mathf.FloorToInt(y) & 255,                  // CONTAINS POINT.
            Z = Mathf.FloorToInt(z) & 255;

        x -= Mathf.Floor(x);                                // FIND RELATIVE X,Y,Z
        y -= Mathf.Floor(y);                                // OF POINT IN CUBE.
        z -= Mathf.Floor(z);
        float u = fade(x),                                // COMPUTE FADE CURVES
              v = fade(y),                                // FOR EACH OF X,Y,Z.
              w = fade(z);
        int A = p[X] + Y, AA = p[A] + Z, AB = p[A + 1] + Z,      // HASH COORDINATES OF
            B = p[X + 1] + Y, BA = p[B] + Z, BB = p[B + 1] + Z;      // THE 8 CUBE CORNERS,

        return lerp(w, lerp(v, lerp(u, grad(p[AA], x, y, z),  // AND ADD
                                       grad(p[BA], x - 1, y, z)), // BLENDED
                               lerp(u, grad(p[AB], x, y - 1, z),  // RESULTS
                                       grad(p[BB], x - 1, y - 1, z))),// FROM  8
                       lerp(v, lerp(u, grad(p[AA + 1], x, y, z - 1),  // CORNERS
                                       grad(p[BA + 1], x - 1, y, z - 1)), // OF CUBE
                               lerp(u, grad(p[AB + 1], x, y - 1, z - 1),
                                       grad(p[BB + 1], x - 1, y - 1, z - 1))));
    }
    float fade(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }
    float lerp(float t, float a, float b) { return a + t * (b - a); }
    float grad(int hash, float x, float y, float z)
    {
        int h = hash & 15;                      // CONVERT LO 4 BITS OF HASH CODE
        float u = h < 8 ? x : y,                 // INTO 12 GRADIENT DIRECTIONS.
               v = h < 4 ? y : h == 12 || h == 14 ? x : z;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }
}
