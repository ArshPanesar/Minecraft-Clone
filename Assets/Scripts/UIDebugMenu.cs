using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDebugMenu : MonoBehaviour
{
    // Terrain Generator
    public TerrainGenerator TerrainGeneratorObject;

    // UI Input Fields
    public TMP_InputField OctavesInputField;
    public TMP_InputField AmpInputField;
    public TMP_InputField AmpGainInputField;
    public TMP_InputField FreqInputField;
    public TMP_InputField FreqGainInputField;
    public TMP_InputField PerlinShiftInputField;
    public TMP_InputField NoiseScaleInputField;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void OnGenerate()
    {
        // Copy Input to Noise Parameters
        if (OctavesInputField.text != "")
        {
            WorldData.TerrainNoiseParam.Octaves = int.Parse(OctavesInputField.text);
        }
        if (PerlinShiftInputField.text != "")
        {
            WorldData.TerrainNoiseParam.PerlinShift = int.Parse(PerlinShiftInputField.text);
        }
        if (AmpInputField.text != "")
        {
            WorldData.TerrainNoiseParam.Amplitude = float.Parse(AmpInputField.text);
        }
        if (AmpGainInputField.text != "")
        {
            WorldData.TerrainNoiseParam.AmplitudeGain = float.Parse(AmpGainInputField.text);
        }
        if (FreqInputField.text != "")
        {
            WorldData.TerrainNoiseParam.Freq = float.Parse(FreqInputField.text);
        }
        if (FreqGainInputField.text != "")
        {
            WorldData.TerrainNoiseParam.FreqGain = float.Parse(FreqGainInputField.text);
        }
        if (NoiseScaleInputField.text != "")
        {
            WorldData.TerrainNoiseParam.NoiseScale = float.Parse(NoiseScaleInputField.text);
        }

        // Remove all loaded Chunks
        TerrainGeneratorObject.RemoveAllChunks();
    }
}
