using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightingPreset Preset;
    [SerializeField, Range(0, 24)] private float TimeOfDay;
    [SerializeField] private float Speed = 1.0f;
    [SerializeField] private Camera GameCamera;

    private bool ToggleCycle = true;

    public void ToggleDayAndNightCycle()
    {
        ToggleCycle = !ToggleCycle;

        if (!ToggleCycle)
        {
            Color SkyBlueColor = new Color(135f / 255f, 206f / 255f, 235f / 255f);
            GameCamera.backgroundColor = SkyBlueColor;
            //RenderSettings.ambientLight = SkyBlueColor;
            RenderSettings.fogColor = SkyBlueColor;

            DirectionalLight.color = Color.white;
            //DirectionalLight.transform.localRotation = Quaternion.Euler(0f, 60f, 0f);
        }
    }

    private void UpdateLighting(float TimePercent)
    {
        RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(TimePercent);
        GameCamera.backgroundColor = Preset.AmbientColor.Evaluate(TimePercent);
        RenderSettings.fogColor = Preset.FogColor.Evaluate(TimePercent);

        if (DirectionalLight != null)
        {
            DirectionalLight.color = Preset.DirectionalColor.Evaluate(TimePercent);
            DirectionalLight.transform.localRotation = Quaternion.Euler(TimePercent * 360f, 170f, 0);
        }
    }

    private void OnValidate()
    {
        if (DirectionalLight != null)
            return;

        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        else
        {
            // Pick First Directional Light Found
            Light[] LightsInScene = GameObject.FindObjectsOfType<Light>();
            foreach (var NewLight in LightsInScene)
            {
                if (NewLight.type == LightType.Directional)
                {
                    DirectionalLight = NewLight;
                    return;
                }
            }
        }
    }

    private void Update()
    {
        if (Preset == null || !ToggleCycle)
            return;

        if (Application.isPlaying)
        {
            TimeOfDay += Time.deltaTime * Speed;
            TimeOfDay %= 24;

            UpdateLighting(TimeOfDay / 24f);
        }
        else
        {
            UpdateLighting(TimeOfDay / 24f);
        }
    }
}
