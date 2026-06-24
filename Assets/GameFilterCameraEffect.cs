using UnityEngine;

/// <summary>
/// Optional camera-level filter pass for effects an overlay cannot truly perform:
/// grayscale, blur, haze, and glow. Uses OnRenderImage, so it works in the built-in render pipeline.
/// For URP, convert this to a ScriptableRendererFeature if OnRenderImage is not called.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class GameFilterCameraEffect : MonoBehaviour
{
    [SerializeField] private Shader filterShader;
    [SerializeField] private bool effectEnabled;

    [Header("Color")]
    [SerializeField, Range(0f, 1f)] private float grayscaleStrength;
    [SerializeField, Range(0f, 2f)] private float saturation = 1f;
    [SerializeField, Range(0.25f, 3f)] private float contrast = 1f;
    [SerializeField, Range(-0.5f, 0.5f)] private float brightness;
    [SerializeField] private Color tintColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float tintStrength;

    [Header("Lens")]
    [SerializeField, Range(0f, 1f)] private float blurStrength;
    [SerializeField, Range(0f, 1.5f)] private float glowStrength;
    [SerializeField] private Color glowColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float hazeStrength;
    [SerializeField] private Color hazeColor = Color.white;

    private Material material;

    private void OnEnable()
    {
        EnsureMaterial();
    }

    private void OnDisable()
    {
        DestroyMaterial();
    }

    public void SetSettings(
        bool enabled,
        float grayscale,
        float saturationAmount,
        float contrastAmount,
        float brightnessAmount,
        Color tint,
        float tintAmount)
    {
        SetSettings(enabled, grayscale, saturationAmount, contrastAmount, brightnessAmount, tint, tintAmount, 0f, 0f, Color.white, 0f, Color.white);
    }

    public void SetSettings(
        bool enabled,
        float grayscale,
        float saturationAmount,
        float contrastAmount,
        float brightnessAmount,
        Color tint,
        float tintAmount,
        float blur,
        float glow,
        Color glowTint,
        float haze,
        Color hazeTint)
    {
        effectEnabled = enabled;
        grayscaleStrength = Mathf.Clamp01(grayscale);
        saturation = Mathf.Clamp(saturationAmount, 0f, 2f);
        contrast = Mathf.Clamp(contrastAmount, 0.25f, 3f);
        brightness = Mathf.Clamp(brightnessAmount, -0.5f, 0.5f);
        tintColor = tint;
        tintStrength = Mathf.Clamp01(tintAmount);
        blurStrength = Mathf.Clamp01(blur);
        glowStrength = Mathf.Clamp(glow, 0f, 1.5f);
        glowColor = glowTint;
        hazeStrength = Mathf.Clamp01(haze);
        hazeColor = hazeTint;

        EnsureMaterial();
    }

    public void ClearSettings()
    {
        effectEnabled = false;
    }

    private void EnsureMaterial()
    {
        if (filterShader == null)
        {
            filterShader = Shader.Find("Hidden/IncrementalDishes/GameFilterCameraEffect");
        }

        if (filterShader == null || material != null)
        {
            return;
        }

        material = new Material(filterShader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!effectEnabled)
        {
            Graphics.Blit(source, destination);
            return;
        }

        EnsureMaterial();

        if (material == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        material.SetFloat("_Grayscale", grayscaleStrength);
        material.SetFloat("_Saturation", saturation);
        material.SetFloat("_Contrast", contrast);
        material.SetFloat("_Brightness", brightness);
        material.SetColor("_TintColor", tintColor);
        material.SetFloat("_TintStrength", tintStrength);
        material.SetFloat("_BlurStrength", blurStrength);
        material.SetFloat("_GlowStrength", glowStrength);
        material.SetColor("_GlowColor", glowColor);
        material.SetFloat("_HazeStrength", hazeStrength);
        material.SetColor("_HazeColor", hazeColor);

        Graphics.Blit(source, destination, material);
    }

    private void DestroyMaterial()
    {
        if (material == null)
        {
            return;
        }

        if (Application.isPlaying)
            Destroy(material);
        else
            DestroyImmediate(material);

        material = null;
    }
}
