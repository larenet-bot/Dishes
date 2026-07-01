using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen visual filter controller.
/// UI overlay layers handle lens-surface details like grease, fog lines, scanlines, and grain.
/// Optional camera post-process handles lens effects that need to alter the rendered image itself,
/// such as real grayscale, blur, haze, and glow.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasGroup))]
public class GameFilterOverlay : MonoBehaviour
{
    public enum FilterPreset
    {
        DirtyGrease = 0,
        SubtleNeon = 1,
        PulsingVioletCRT = 2,
        StaticColor = 3,
        Clean = 4,
        Noir = 5,
        LateShiftNeon = 6,
        HeatLamp = 7,
        SteamFog = 8,
        ArcadeScanlines = 9,
        DishwasherDream = 10
    }

    public enum ProceduralLensMode
    {
        Off = 0,
        GreaseCornerBlobs = 1,
        LavaLampBlobs = 2,
        RollingFogLines = 3,
        DreamHaze = 4,
        NeonGlow = 5,
        ClassicNeonGlow = 6,
        FixedTriColorNeonGlow = 7
    }

    [System.Serializable]
    public class FilterSettings
    {
        [Header("Color Wash")]
        public Color colorWash = new Color(0f, 0f, 0f, 0f);
        public bool pulseColorWashTowardTarget = false;
        public Color colorWashPulseTarget = new Color(0f, 0f, 0f, 0f);
        [Range(0f, 1f)] public float colorPulseStrength = 0f;
        public float colorPulseSpeed = 0.25f;

        [Header("Procedural Lens Layer")]
        public ProceduralLensMode proceduralMode = ProceduralLensMode.Off;
        public Color proceduralColorA = new Color(0.40f, 0.17f, 0.035f, 1f);
        public Color proceduralColorB = new Color(0.12f, 0.045f, 0.005f, 1f);
        [Range(0f, 1f)] public float proceduralAlpha = 0f;
        [Range(0.25f, 8f)] public float proceduralScale = 1f;
        [Range(0f, 4f)] public float proceduralSpeed = 1f;
        [Range(0f, 3f)] public float proceduralIntensity = 1f;
        public Vector2 proceduralDirection = new Vector2(-1f, 0f);

        [Header("Soft Overlay Glow")]
        public bool useOverlayGlow = false;
        public Color overlayGlowColorA = new Color(0.00f, 0.70f, 0.85f, 1f);
        public Color overlayGlowColorB = new Color(0.80f, 0.05f, 0.55f, 1f);
        [Range(0f, 0.35f)] public float overlayGlowAlpha = 0f;
        [Range(0f, 0.20f)] public float overlayGlowPulseStrength = 0f;
        [Range(0f, 3f)] public float overlayGlowPulseSpeed = 0.5f;
        [Range(0f, 0.20f)] public float overlayGlowFlickerStrength = 0f;
        [Range(0f, 6f)] public float overlayGlowFlickerSpeed = 1f;

        [Header("Water Spots")]
        public bool useWaterSpots = false;
        public Color waterSpotColor = new Color(0.70f, 0.86f, 0.88f, 1f);
        [Range(0f, 0.65f)] public float waterSpotAlpha = 0f;
        [Range(0, 60)] public int waterSpotCount = 16;
        [Range(0.02f, 0.35f)] public float waterSpotMinRadius = 0.045f;
        [Range(0.02f, 0.45f)] public float waterSpotMaxRadius = 0.15f;
        [Range(0.002f, 0.08f)] public float waterSpotRingThickness = 0.018f;
        public Vector2 waterSpotDriftSpeed = new Vector2(-0.002f, 0.001f);

        [Header("Lines / Streaks")]
        public bool useStreaks = false;
        public Color streakColor = new Color(1f, 1f, 1f, 1f);
        [Range(0f, 0.9f)] public float streakAlpha = 0f;
        [Range(0, 48)] public int streakCount = 14;
        [Range(0.004f, 0.20f)] public float streakWidth = 0.02f;
        [Range(0.10f, 2.00f)] public float streakLength = 1.00f;
        [Range(-90f, 90f)] public float streakAngle = 0f;
        public Vector2 streakDriftSpeed = new Vector2(0.01f, 0f);

        [Header("Scanlines")]
        [Range(0f, 0.6f)] public float scanlineAlpha = 0f;
        [Min(2)] public int scanlineSpacingPixels = 4;
        public float scanlineScrollSpeed = 0.05f;

        [Header("Noise / Grain")]
        [Range(0f, 0.45f)] public float noiseAlpha = 0f;
        [Range(16, 256)] public int noiseTextureSize = 64;
        [Min(1f)] public float noiseRefreshesPerSecond = 4f;
        public float noiseDriftSpeed = 0.012f;

        [Header("Vignette")]
        public Color vignetteColor = new Color(0f, 0f, 0f, 1f);
        [Range(0f, 0.85f)] public float vignetteAlpha = 0f;
        [Range(0f, 1f)] public float vignetteInnerRadius = 0.35f;

        [Header("Camera Lens / Post-Process")]
        public bool useCameraPostProcess = false;
        [Range(0f, 1f)] public float grayscaleStrength = 0f;
        [Range(0f, 2f)] public float saturation = 1f;
        [Range(0.25f, 3f)] public float contrast = 1f;
        [Range(-0.5f, 0.5f)] public float brightness = 0f;
        public Color tintColor = Color.white;
        [Range(0f, 1f)] public float tintStrength = 0f;
        [Range(0f, 1f)] public float blurStrength = 0f;
        [Range(0f, 1.5f)] public float glowStrength = 0f;
        public Color glowColor = Color.white;
        [Range(0f, 1f)] public float hazeStrength = 0f;
        public Color hazeColor = Color.white;

        public FilterSettings Clone()
        {
            return (FilterSettings)MemberwiseClone();
        }
    }

    public static GameFilterOverlay Instance { get; private set; }

    [Header("Saved On/Off Setting")]
    [SerializeField] private bool filterEnabled = true;
    [SerializeField] private bool saveEnabledPreference = true;
    [SerializeField] private string enabledPrefsKey = "GameFilterOverlayEnabled";

    [Header("Preset")]
    [SerializeField] private FilterPreset currentPreset = FilterPreset.DirtyGrease;
    [Tooltip("Leave this off when another system, such as MopUpgradeShelf, owns the selected preset.")]
    [SerializeField] private bool savePresetPreference = false;
    [SerializeField] private string presetPrefsKey = "GameFilterOverlayPreset";
    [SerializeField] private bool previewPresetInInspector = true;

    [Header("Canvas")]
    [SerializeField] private int sortingOrder = 32767;
    [SerializeField] private bool dontDestroyOnLoad = false;

    [Header("Camera Lens / Post-Process")]
    [Tooltip("Camera-rendered grayscale, blur, haze, and camera-level glow. Screen Space Overlay Image components can also be handled by the UI Image Lens below.")]
    [SerializeField] private bool allowCameraPostProcess = true;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private GameFilterCameraEffect cameraEffect;
    [SerializeField] private bool autoAddCameraEffect = true;

    [Header("UI Image Lens")]
    [Tooltip("Also applies grayscale, tint, blur, and haze to Image/RawImage components. This helps filters affect Screen Space Overlay UI artwork.")]
    [SerializeField] private bool allowUiImageLens = true;
    [SerializeField] private bool includeInactiveUiImages = false;
    [SerializeField, Min(0.05f)] private float uiImageLensRefreshInterval = 0.75f;
    [Tooltip("Canvases assigned here will not have their Image/RawImage materials replaced by the lens effect. Add menu canvases here if you want menus to stay clean.")]
    [SerializeField] private Canvas[] uiImageLensExcludedCanvases;

    [Header("Sprite Renderer Lens")]
    [Tooltip("Also applies the lens material to SpriteRenderer objects so Noir and blur can affect 2D sprites outside UI canvases.")]
    [SerializeField] private bool allowSpriteRendererLens = true;
    [SerializeField] private bool includeInactiveSpriteRenderers = false;

    [Header("Preset Default Upgrade")]
    [SerializeField] private bool autoApplyPresetDefaultUpgrades = true;
    [SerializeField, HideInInspector] private int presetDefaultsVersion = 0;
    private const int CurrentPresetDefaultsVersion = 10;

    [Header("Preset Settings")]
    [SerializeField] private FilterSettings dirtyGrease = CreateDirtyGreaseDefaults();
    [SerializeField] private FilterSettings subtleNeon = CreateSubtleNeonDefaults();
    [SerializeField] private FilterSettings pulsingVioletCRT = CreatePulsingVioletCRTDefaults();
    [SerializeField] private FilterSettings clean = CreateCleanDefaults();
    [SerializeField] private FilterSettings noir = CreateNoirDefaults();
    [SerializeField] private FilterSettings lateShiftNeon = CreateLateShiftNeonDefaults();
    [SerializeField] private FilterSettings heatLamp = CreateHeatLampDefaults();
    [SerializeField] private FilterSettings steamFog = CreateSteamFogDefaults();
    [SerializeField] private FilterSettings arcadeScanlines = CreateArcadeScanlinesDefaults();
    [SerializeField] private FilterSettings dishwasherDream = CreateDishwasherDreamDefaults();
    [SerializeField] private FilterSettings staticColor = CreateStaticColorDefaults();

    [Header("Generated UI Parts")]
    [SerializeField] private Image colorWashImage;
    [SerializeField] private RawImage proceduralLensImage;
    [SerializeField] private RawImage overlayGlowImage;
    [SerializeField] private RawImage waterSpotImage;
    [SerializeField] private RawImage streakImage;
    [SerializeField] private RawImage scanlineImage;
    [SerializeField] private RawImage noiseImage;
    [SerializeField] private RawImage vignetteImage;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private GraphicRaycaster graphicRaycaster;
    private Texture2D overlayGlowTexture;
    private Texture2D waterSpotTexture;
    private Texture2D streakTexture;
    private Texture2D scanlineTexture;
    private Texture2D noiseTexture;
    private Texture2D vignetteTexture;
    private Material proceduralMaterial;
    private Material uiImageLensMaterial;
    private Material spriteLensMaterial;
    private readonly Dictionary<Graphic, Material> originalUiImageMaterials = new Dictionary<Graphic, Material>();
    private readonly Dictionary<SpriteRenderer, Material> originalSpriteRendererMaterials = new Dictionary<SpriteRenderer, Material>();
    private readonly Dictionary<SpriteRenderer, Material> activeSpriteRendererLensMaterials = new Dictionary<SpriteRenderer, Material>();
    private FilterSettings activeSettings;
    private float nextNoiseRefreshTime;
    private float nextUiImageLensRefreshTime;
    private float nextSpriteRendererLensRefreshTime;
    private int cachedScreenWidth;
    private int cachedScreenHeight;

    private FilterSettings Active
    {
        get
        {
            if (activeSettings == null)
            {
                RefreshActiveSettingsFromPreset();
            }

            return activeSettings;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheComponents();
        SetupCanvas();
        BuildGeneratedUIIfNeeded();
        ApplyPresetDefaultUpgradeIfNeeded();

        if (saveEnabledPreference && PlayerPrefs.HasKey(enabledPrefsKey))
        {
            filterEnabled = PlayerPrefs.GetInt(enabledPrefsKey, filterEnabled ? 1 : 0) == 1;
        }

        if (savePresetPreference && PlayerPrefs.HasKey(presetPrefsKey))
        {
            currentPreset = (FilterPreset)PlayerPrefs.GetInt(presetPrefsKey, (int)currentPreset);
        }

        RefreshActiveSettingsFromPreset();
        RebuildTextures();
        ApplyFilterState();
        ApplyCameraPostProcess();

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnValidate()
    {
        CacheComponents();
        SetupCanvas();
        ApplyPresetDefaultUpgradeIfNeeded();

        if (previewPresetInInspector)
        {
            RefreshActiveSettingsFromPreset();
        }

        if (Application.isPlaying)
        {
            BuildGeneratedUIIfNeeded();
            RebuildTextures();
            ApplyFilterState();
            ApplyCameraPostProcess();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        RestoreUiImageLensMaterials();
        RestoreSpriteRendererLensMaterials();
        DestroyRuntimeMaterial();
        DestroyUiImageLensMaterial();
        DestroySpriteLensMaterial();
    }

    private void Update()
    {
        if (!filterEnabled)
        {
            return;
        }

        UpdateScreenTilingIfNeeded();
        UpdateColorPulse();
        UpdateProceduralLens();
        UpdateOverlayGlow();
        UpdateTextureLayer(waterSpotImage, Active.waterSpotDriftSpeed);
        UpdateTextureLayer(streakImage, Active.streakDriftSpeed);
        UpdateScanlines();
        UpdateNoise();
        UpdateUiImageLensPeriodically();
        UpdateSpriteRendererLensPeriodically();
    }

    private void CacheComponents()
    {
        if (canvas == null)
            canvas = GetComponent<Canvas>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (graphicRaycaster == null)
            graphicRaycaster = GetComponent<GraphicRaycaster>();
    }

    private void SetupCanvas()
    {
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.ignoreParentGroups = true;
        }

        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = false;
        }
    }

    private void BuildGeneratedUIIfNeeded()
    {
        colorWashImage = GetOrCreateImage(colorWashImage, "Filter_ColorWash");
        proceduralLensImage = GetOrCreateRawImage(proceduralLensImage, "Filter_ProceduralLens");
        overlayGlowImage = GetOrCreateRawImage(overlayGlowImage, "Filter_OverlayGlow");
        waterSpotImage = GetOrCreateRawImage(waterSpotImage, "Filter_WaterSpots");
        streakImage = GetOrCreateRawImage(streakImage, "Filter_Streaks");
        scanlineImage = GetOrCreateRawImage(scanlineImage, "Filter_Scanlines");
        noiseImage = GetOrCreateRawImage(noiseImage, "Filter_Noise");
        vignetteImage = GetOrCreateRawImage(vignetteImage, "Filter_Vignette");

        DisableLegacyLayer("Filter_Stains");
        DisableLegacyLayer("Filter_AccentGlow");
        DisableLegacyLayer("Filter_GreaseStreaks");

        colorWashImage.transform.SetSiblingIndex(0);
        overlayGlowImage.transform.SetSiblingIndex(1);
        proceduralLensImage.transform.SetSiblingIndex(2);
        waterSpotImage.transform.SetSiblingIndex(3);
        streakImage.transform.SetSiblingIndex(4);
        scanlineImage.transform.SetSiblingIndex(5);
        noiseImage.transform.SetSiblingIndex(6);
        vignetteImage.transform.SetSiblingIndex(7);

        DisableRaycasts(colorWashImage);
        DisableRaycasts(overlayGlowImage);
        DisableRaycasts(proceduralLensImage);
        DisableRaycasts(waterSpotImage);
        DisableRaycasts(streakImage);
        DisableRaycasts(scanlineImage);
        DisableRaycasts(noiseImage);
        DisableRaycasts(vignetteImage);

        EnsureProceduralMaterial();
    }

    private void DisableRaycasts(Graphic graphic)
    {
        if (graphic != null)
        {
            graphic.raycastTarget = false;
        }
    }

    private void DisableLegacyLayer(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private Image GetOrCreateImage(Image existing, string childName)
    {
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            StretchToFullScreen(existing.rectTransform);
            return existing;
        }

        Transform found = transform.Find(childName);
        if (found != null && found.TryGetComponent(out Image foundImage))
        {
            foundImage.gameObject.SetActive(true);
            StretchToFullScreen(foundImage.rectTransform);
            return foundImage;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(transform, false);
        Image image = child.GetComponent<Image>();
        StretchToFullScreen(image.rectTransform);
        return image;
    }

    private RawImage GetOrCreateRawImage(RawImage existing, string childName)
    {
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            StretchToFullScreen(existing.rectTransform);
            return existing;
        }

        Transform found = transform.Find(childName);
        if (found != null && found.TryGetComponent(out RawImage foundImage))
        {
            foundImage.gameObject.SetActive(true);
            StretchToFullScreen(foundImage.rectTransform);
            return foundImage;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform), typeof(RawImage));
        child.transform.SetParent(transform, false);
        RawImage rawImage = child.GetComponent<RawImage>();
        StretchToFullScreen(rawImage.rectTransform);
        return rawImage;
    }

    private void StretchToFullScreen(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void EnsureProceduralMaterial()
    {
        if (proceduralLensImage == null)
        {
            return;
        }

        if (proceduralMaterial == null)
        {
            Shader shader = Shader.Find("Hidden/IncrementalDishes/GameFilterProceduralLayer");
            if (shader != null)
            {
                proceduralMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
        }

        proceduralLensImage.texture = Texture2D.whiteTexture;
        proceduralLensImage.material = proceduralMaterial;
        proceduralLensImage.color = Color.white;
    }

    private void DestroyRuntimeMaterial()
    {
        if (proceduralMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
            Destroy(proceduralMaterial);
        else
            DestroyImmediate(proceduralMaterial);

        proceduralMaterial = null;
    }

    private void EnsureUiImageLensMaterial()
    {
        if (uiImageLensMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Hidden/IncrementalDishes/GameFilterUIElementLens");
        if (shader == null)
        {
            return;
        }

        uiImageLensMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void DestroyUiImageLensMaterial()
    {
        if (uiImageLensMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
            Destroy(uiImageLensMaterial);
        else
            DestroyImmediate(uiImageLensMaterial);

        uiImageLensMaterial = null;
    }

    private void EnsureSpriteLensMaterial()
    {
        if (spriteLensMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Hidden/IncrementalDishes/GameFilterSpriteLens");
        if (shader == null)
        {
            return;
        }

        spriteLensMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
    }

    private void DestroySpriteLensMaterial()
    {
        if (spriteLensMaterial == null)
        {
            return;
        }

        if (Application.isPlaying)
            Destroy(spriteLensMaterial);
        else
            DestroyImmediate(spriteLensMaterial);

        spriteLensMaterial = null;
    }

    private void ApplyPresetDefaultUpgradeIfNeeded()
    {
        if (!autoApplyPresetDefaultUpgrades || presetDefaultsVersion >= CurrentPresetDefaultsVersion)
        {
            return;
        }

        ResetPresetSettingsToLensDefaults();
    }

    [ContextMenu("Reset Preset Settings To Lens Defaults")]
    public void ResetPresetSettingsToLensDefaults()
    {
        dirtyGrease = CreateDirtyGreaseDefaults();
        subtleNeon = CreateSubtleNeonDefaults();
        pulsingVioletCRT = CreatePulsingVioletCRTDefaults();
        clean = CreateCleanDefaults();
        noir = CreateNoirDefaults();
        lateShiftNeon = CreateLateShiftNeonDefaults();
        heatLamp = CreateHeatLampDefaults();
        steamFog = CreateSteamFogDefaults();
        arcadeScanlines = CreateArcadeScanlinesDefaults();
        dishwasherDream = CreateDishwasherDreamDefaults();
        staticColor = CreateStaticColorDefaults();
        presetDefaultsVersion = CurrentPresetDefaultsVersion;

        RefreshActiveSettingsFromPreset();

        if (Application.isPlaying)
        {
            BuildGeneratedUIIfNeeded();
            RebuildTextures();
            ApplyFilterState();
            ApplyCameraPostProcess();
        }
    }

    private FilterSettings GetPresetSettings(FilterPreset preset)
    {
        switch (preset)
        {
            case FilterPreset.Clean:
                return clean;
            case FilterPreset.SubtleNeon:
                return subtleNeon;
            case FilterPreset.PulsingVioletCRT:
                return pulsingVioletCRT;
            case FilterPreset.StaticColor:
                return staticColor;
            case FilterPreset.Noir:
                return noir;
            case FilterPreset.LateShiftNeon:
                return lateShiftNeon;
            case FilterPreset.HeatLamp:
                return heatLamp;
            case FilterPreset.SteamFog:
                return steamFog;
            case FilterPreset.ArcadeScanlines:
                return arcadeScanlines;
            case FilterPreset.DishwasherDream:
                return dishwasherDream;
            case FilterPreset.DirtyGrease:
            default:
                return dirtyGrease;
        }
    }

    private void RefreshActiveSettingsFromPreset()
    {
        FilterSettings presetSettings = GetPresetSettings(currentPreset);
        activeSettings = presetSettings != null ? presetSettings.Clone() : new FilterSettings();
    }

    private void RebuildTextures()
    {
        CreateOverlayGlowTexture();
        CreateWaterSpotTexture();
        CreateStreakTexture();
        CreateScanlineTexture();
        CreateNoiseTexture();
        CreateVignetteTexture();
        ApplyBaseVisuals();
        UpdateScreenTilingIfNeeded(force: true);
    }

    private void ApplyBaseVisuals()
    {
        if (colorWashImage != null)
        {
            colorWashImage.color = Active.colorWash;
        }

        ApplyLayer(overlayGlowImage, overlayGlowTexture, Color.white, Active.useOverlayGlow ? Active.overlayGlowAlpha : 0f);
        ApplyLayer(waterSpotImage, waterSpotTexture, Active.waterSpotColor, Active.useWaterSpots ? Active.waterSpotAlpha : 0f);
        ApplyLayer(streakImage, streakTexture, Active.streakColor, Active.useStreaks ? Active.streakAlpha : 0f);

        if (scanlineImage != null)
        {
            scanlineImage.texture = scanlineTexture;
            scanlineImage.color = new Color(0f, 0f, 0f, Active.scanlineAlpha);
        }

        if (noiseImage != null)
        {
            noiseImage.texture = noiseTexture;
            noiseImage.color = new Color(1f, 1f, 1f, Active.noiseAlpha);
        }

        if (vignetteImage != null)
        {
            vignetteImage.texture = vignetteTexture;
            Color vignette = Active.vignetteColor;
            vignette.a = Active.vignetteAlpha;
            vignetteImage.color = vignette;
        }

        UpdateProceduralLens();
    }

    private void ApplyLayer(RawImage image, Texture2D texture, Color color, float alpha)
    {
        if (image == null)
        {
            return;
        }

        image.texture = texture;
        Color layerColor = color;
        layerColor.a = Mathf.Clamp01(alpha);
        image.color = layerColor;
        image.rectTransform.localScale = Vector3.one;
    }

    private void UpdateProceduralLens()
    {
        if (proceduralLensImage == null)
        {
            return;
        }

        EnsureProceduralMaterial();

        if (proceduralMaterial == null || Active.proceduralMode == ProceduralLensMode.Off || Active.proceduralAlpha <= 0f)
        {
            proceduralLensImage.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        proceduralLensImage.color = Color.white;
        proceduralLensImage.rectTransform.localScale = Vector3.one;
        proceduralLensImage.uvRect = new Rect(0f, 0f, 1f, 1f);

        Vector2 dir = Active.proceduralDirection.sqrMagnitude <= 0.001f ? Vector2.right : Active.proceduralDirection.normalized;
        proceduralMaterial.SetFloat("_Mode", (float)Active.proceduralMode);
        proceduralMaterial.SetColor("_ColorA", Active.proceduralColorA);
        proceduralMaterial.SetColor("_ColorB", Active.proceduralColorB);
        proceduralMaterial.SetFloat("_Alpha", Active.proceduralAlpha);
        proceduralMaterial.SetFloat("_Scale", Active.proceduralScale);
        proceduralMaterial.SetFloat("_Speed", Active.proceduralSpeed);
        proceduralMaterial.SetFloat("_Intensity", Active.proceduralIntensity);
        proceduralMaterial.SetVector("_Direction", new Vector4(dir.x, dir.y, 0f, 0f));
    }

    private void CreateOverlayGlowTexture()
    {
        DestroyTexture(overlayGlowTexture);
        const int size = 256;
        overlayGlowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 leftGlow = new Vector2(0.18f, 0.12f);
        Vector2 rightGlow = new Vector2(0.86f, 0.10f);
        Vector2 lowerGlow = new Vector2(0.55f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));

                float left = 1f - Mathf.Clamp01(Vector2.Distance(uv, leftGlow) / 0.75f);
                float right = 1f - Mathf.Clamp01(Vector2.Distance(uv, rightGlow) / 0.75f);
                float lower = 1f - Mathf.Clamp01(Vector2.Distance(uv, lowerGlow) / 0.90f);

                left *= left;
                right *= right;
                lower = lower * lower * 0.55f;

                Color mixed = (Active.overlayGlowColorA * left) + (Active.overlayGlowColorB * right) + (Color.Lerp(Active.overlayGlowColorA, Active.overlayGlowColorB, 0.5f) * lower);
                float alpha = Mathf.Clamp01((left + right + lower) * 0.85f);
                mixed.a = alpha;
                overlayGlowTexture.SetPixel(x, y, mixed);
            }
        }

        overlayGlowTexture.Apply();
    }

    private void CreateWaterSpotTexture()
    {
        DestroyTexture(waterSpotTexture);
        const int size = 256;
        waterSpotTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        int count = Mathf.Max(0, Active.waterSpotCount);
        float minRadius = Mathf.Min(Active.waterSpotMinRadius, Active.waterSpotMaxRadius);
        float maxRadius = Mathf.Max(Active.waterSpotMinRadius, Active.waterSpotMaxRadius);
        float thickness = Mathf.Max(0.001f, Active.waterSpotRingThickness);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float intensity = 0f;

                for (int i = 0; i < count; i++)
                {
                    Vector2 center = new Vector2(PseudoRandom01(i, 121.4f), PseudoRandom01(i, 230.9f));
                    float radius = Mathf.Lerp(minRadius, maxRadius, PseudoRandom01(i, 314.1f));
                    float distance = Vector2.Distance(uv, center);
                    float ring = 1f - Mathf.SmoothStep(0f, thickness, Mathf.Abs(distance - radius));
                    float faintFill = (1f - Mathf.SmoothStep(0f, radius, distance)) * 0.24f;
                    intensity = Mathf.Max(intensity, Mathf.Max(ring, faintFill));
                }

                intensity = Mathf.Clamp01(intensity);
                waterSpotTexture.SetPixel(x, y, new Color(1f, 1f, 1f, intensity));
            }
        }

        waterSpotTexture.Apply();
    }

    private void CreateStreakTexture()
    {
        DestroyTexture(streakTexture);
        const int size = 256;
        streakTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        int count = Mathf.Max(0, Active.streakCount);
        float angle = Active.streakAngle * Mathf.Deg2Rad;
        Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        Vector2 normal = new Vector2(-axis.y, axis.x);
        float halfLength = Mathf.Max(0.01f, Active.streakLength * 0.5f);
        float halfWidth = Mathf.Max(0.002f, Active.streakWidth * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float intensity = 0f;

                for (int i = 0; i < count; i++)
                {
                    Vector2 center = new Vector2(PseudoRandom01(i, 412.3f), PseudoRandom01(i, 625.7f));
                    Vector2 delta = uv - center;
                    float along = Mathf.Abs(Vector2.Dot(delta, axis));
                    float across = Mathf.Abs(Vector2.Dot(delta, normal));
                    float lengthFade = 1f - Mathf.SmoothStep(halfLength * 0.55f, halfLength, along);
                    float widthFade = 1f - Mathf.SmoothStep(0f, halfWidth, across);
                    float streakNoise = Mathf.PerlinNoise((uv.x + i) * 16f, (uv.y - i) * 5f);
                    intensity = Mathf.Max(intensity, lengthFade * widthFade * Mathf.Lerp(0.35f, 1f, streakNoise));
                }

                intensity = Mathf.Clamp01(intensity);
                streakTexture.SetPixel(x, y, new Color(1f, 1f, 1f, intensity));
            }
        }

        streakTexture.Apply();
    }

    private void CreateScanlineTexture()
    {
        DestroyTexture(scanlineTexture);
        int height = Mathf.Max(2, Active.scanlineSpacingPixels);
        scanlineTexture = new Texture2D(1, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        for (int y = 0; y < height; y++)
        {
            float alpha = y == 0 ? 1f : 0f;
            scanlineTexture.SetPixel(0, y, new Color(0f, 0f, 0f, alpha));
        }

        scanlineTexture.Apply();
    }

    private void CreateNoiseTexture()
    {
        DestroyTexture(noiseTexture);
        int size = Mathf.Clamp(Active.noiseTextureSize, 16, 256);
        noiseTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };

        RefreshNoiseTexture();
    }

    private void RefreshNoiseTexture()
    {
        if (noiseTexture == null)
        {
            return;
        }

        for (int y = 0; y < noiseTexture.height; y++)
        {
            for (int x = 0; x < noiseTexture.width; x++)
            {
                float value = Random.Range(0.20f, 1f);
                float alpha = Random.Range(0f, 1f);
                noiseTexture.SetPixel(x, y, new Color(value, value, value, alpha));
            }
        }

        noiseTexture.Apply();
    }

    private void CreateVignetteTexture()
    {
        DestroyTexture(vignetteTexture);
        const int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(0.5f, 0.5f);
        float inner = Mathf.Clamp01(Active.vignetteInnerRadius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 uv = new Vector2(x / (float)(size - 1), y / (float)(size - 1));
                float distance = Vector2.Distance(uv, center) / 0.7071f;
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, 1f, distance));
                vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        vignetteTexture.Apply();
    }

    private void UpdateColorPulse()
    {
        if (colorWashImage == null)
        {
            return;
        }

        float wave = Active.colorPulseSpeed <= 0f ? 0f : Mathf.Sin(Time.unscaledTime * Active.colorPulseSpeed * Mathf.PI * 2f);
        Color color = Active.colorWash;

        if (Active.pulseColorWashTowardTarget)
        {
            float blend = Mathf.Clamp01((wave * 0.5f + 0.5f) * Active.colorPulseStrength);
            color = Color.Lerp(Active.colorWash, Active.colorWashPulseTarget, blend);
        }
        else
        {
            color.a = Mathf.Clamp01(color.a + wave * Active.colorPulseStrength);
        }

        colorWashImage.color = color;
    }

    private void UpdateTextureLayer(RawImage image, Vector2 driftSpeed)
    {
        if (image == null)
        {
            return;
        }

        image.rectTransform.localScale = Vector3.one;
        Rect uv = image.uvRect;
        uv.x = Time.unscaledTime * driftSpeed.x;
        uv.y = Time.unscaledTime * driftSpeed.y;
        image.uvRect = uv;
    }

    private void UpdateOverlayGlow()
    {
        if (overlayGlowImage == null)
        {
            return;
        }

        if (!Active.useOverlayGlow || Active.overlayGlowAlpha <= 0f)
        {
            overlayGlowImage.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        float pulse = Active.overlayGlowPulseSpeed <= 0f
            ? 0f
            : Mathf.Sin((Time.unscaledTime * Active.overlayGlowPulseSpeed + 0.33f) * Mathf.PI * 2f);

        float flicker = 0f;
        if (Active.overlayGlowFlickerStrength > 0f && Active.overlayGlowFlickerSpeed > 0f)
        {
            float nA = Mathf.PerlinNoise(Time.unscaledTime * Active.overlayGlowFlickerSpeed, 0.31f);
            float nB = Mathf.PerlinNoise(Time.unscaledTime * Active.overlayGlowFlickerSpeed * 3.7f, 6.19f);
            flicker = ((nA * 0.7f) + (nB * 0.3f) - 0.5f) * 2f * Active.overlayGlowFlickerStrength;
        }

        float alpha = Mathf.Clamp01(Active.overlayGlowAlpha + pulse * Active.overlayGlowPulseStrength + flicker);
        overlayGlowImage.color = new Color(1f, 1f, 1f, alpha);
    }

    private void UpdateScanlines()
    {
        if (scanlineImage == null)
        {
            return;
        }

        Rect uv = scanlineImage.uvRect;
        uv.y = Time.unscaledTime * Active.scanlineScrollSpeed;
        scanlineImage.uvRect = uv;
        scanlineImage.color = new Color(0f, 0f, 0f, Active.scanlineAlpha);
    }

    private void UpdateNoise()
    {
        if (noiseImage == null)
        {
            return;
        }

        if (Active.noiseAlpha <= 0f)
        {
            noiseImage.color = new Color(1f, 1f, 1f, 0f);
            return;
        }

        if (Time.unscaledTime >= nextNoiseRefreshTime)
        {
            RefreshNoiseTexture();
            nextNoiseRefreshTime = Time.unscaledTime + (1f / Mathf.Max(1f, Active.noiseRefreshesPerSecond));
        }

        Rect uv = noiseImage.uvRect;
        uv.x = Time.unscaledTime * Active.noiseDriftSpeed;
        uv.y = Time.unscaledTime * Active.noiseDriftSpeed * -0.75f;
        noiseImage.uvRect = uv;
        noiseImage.color = new Color(1f, 1f, 1f, Active.noiseAlpha);
    }

    private void UpdateScreenTilingIfNeeded(bool force = false)
    {
        if (!force && cachedScreenWidth == Screen.width && cachedScreenHeight == Screen.height)
        {
            return;
        }

        cachedScreenWidth = Screen.width;
        cachedScreenHeight = Screen.height;

        SetFullScreenUv(proceduralLensImage);
        SetFullScreenUv(overlayGlowImage);
        SetFullScreenUv(waterSpotImage);
        SetFullScreenUv(streakImage);

        if (scanlineImage != null)
        {
            float lineHeight = Mathf.Max(2, Active.scanlineSpacingPixels);
            scanlineImage.uvRect = new Rect(0f, scanlineImage.uvRect.y, 1f, Screen.height / lineHeight);
        }

        if (noiseImage != null && noiseTexture != null)
        {
            float xTiles = Mathf.Max(1f, Screen.width / (float)noiseTexture.width);
            float yTiles = Mathf.Max(1f, Screen.height / (float)noiseTexture.height);
            noiseImage.uvRect = new Rect(noiseImage.uvRect.x, noiseImage.uvRect.y, xTiles, yTiles);
        }

        if (vignetteImage != null)
        {
            vignetteImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        }
    }

    private void SetFullScreenUv(RawImage image)
    {
        if (image != null)
        {
            image.uvRect = new Rect(0f, 0f, 1f, 1f);
            image.rectTransform.localScale = Vector3.one;
        }
    }

    private void ApplyCameraPostProcess()
    {
        bool useLens = filterEnabled && Active.useCameraPostProcess;

        if (!allowCameraPostProcess || !useLens)
        {
            if (cameraEffect != null)
            {
                cameraEffect.ClearSettings();
            }
        }
        else
        {
            EnsureCameraEffect();

            if (cameraEffect != null)
            {
                cameraEffect.SetSettings(
                    true,
                    Active.grayscaleStrength,
                    Active.saturation,
                    Active.contrast,
                    Active.brightness,
                    Active.tintColor,
                    Active.tintStrength,
                    Active.blurStrength,
                    Active.glowStrength,
                    Active.glowColor,
                    Active.hazeStrength,
                    Active.hazeColor
                );
            }
        }

        ApplyUiImageLens();
        ApplySpriteRendererLens();
    }


    private void UpdateUiImageLensPeriodically()
    {
        if (!Application.isPlaying || Time.unscaledTime < nextUiImageLensRefreshTime)
        {
            return;
        }

        nextUiImageLensRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, uiImageLensRefreshInterval);
        ApplyUiImageLens();
    }

    private bool ShouldUseUiImageLens()
    {
        if (!allowUiImageLens || !filterEnabled || !Active.useCameraPostProcess)
        {
            return false;
        }

        return Active.grayscaleStrength > 0.001f
            || Mathf.Abs(Active.saturation - 1f) > 0.001f
            || Mathf.Abs(Active.contrast - 1f) > 0.001f
            || Mathf.Abs(Active.brightness) > 0.001f
            || Active.tintStrength > 0.001f
            || Active.blurStrength > 0.001f
            || Active.hazeStrength > 0.001f
            || Active.glowStrength > 0.001f;
    }

    private void ApplyUiImageLens()
    {
        if (!ShouldUseUiImageLens())
        {
            RestoreUiImageLensMaterials();
            return;
        }

        EnsureUiImageLensMaterial();
        if (uiImageLensMaterial == null)
        {
            return;
        }

        uiImageLensMaterial.SetFloat("_Grayscale", Active.grayscaleStrength);
        uiImageLensMaterial.SetFloat("_Saturation", Active.saturation);
        uiImageLensMaterial.SetFloat("_Contrast", Active.contrast);
        uiImageLensMaterial.SetFloat("_Brightness", Active.brightness);
        uiImageLensMaterial.SetColor("_TintColor", Active.tintColor);
        uiImageLensMaterial.SetFloat("_TintStrength", Active.tintStrength);
        uiImageLensMaterial.SetFloat("_BlurStrength", Active.blurStrength);
        uiImageLensMaterial.SetFloat("_GlowStrength", Active.glowStrength);
        uiImageLensMaterial.SetColor("_GlowColor", Active.glowColor);
        uiImageLensMaterial.SetFloat("_HazeStrength", Active.hazeStrength);
        uiImageLensMaterial.SetColor("_HazeColor", Active.hazeColor);

        Graphic[] graphics = includeInactiveUiImages
            ? Resources.FindObjectsOfTypeAll<Graphic>()
            : FindObjectsOfType<Graphic>();

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (ShouldSkipUiImageLensGraphic(graphic))
            {
                continue;
            }

            if (!originalUiImageMaterials.ContainsKey(graphic))
            {
                originalUiImageMaterials.Add(graphic, graphic.material);
            }

            if (graphic.material != uiImageLensMaterial)
            {
                graphic.material = uiImageLensMaterial;
            }
        }

        RemoveMissingUiImageLensEntries();
    }

    private bool ShouldSkipUiImageLensGraphic(Graphic graphic)
    {
        if (graphic == null || !graphic.gameObject.scene.IsValid())
        {
            return true;
        }

        if (graphic.transform.IsChildOf(transform))
        {
            return true;
        }

        Canvas graphicCanvas = graphic.canvas;
        if (graphicCanvas != null && canvas != null && graphicCanvas == canvas)
        {
            return true;
        }

        if (uiImageLensExcludedCanvases != null)
        {
            for (int i = 0; i < uiImageLensExcludedCanvases.Length; i++)
            {
                Canvas excluded = uiImageLensExcludedCanvases[i];
                if (excluded != null && graphic.transform.IsChildOf(excluded.transform))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void UpdateSpriteRendererLensPeriodically()
    {
        if (!Application.isPlaying || Time.unscaledTime < nextSpriteRendererLensRefreshTime)
        {
            return;
        }

        nextSpriteRendererLensRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, uiImageLensRefreshInterval);
        ApplySpriteRendererLens();
    }

    private bool ShouldUseSpriteRendererLens()
    {
        if (!allowSpriteRendererLens || !filterEnabled || !Active.useCameraPostProcess)
        {
            return false;
        }

        // Keep SpriteRenderer material replacement narrow. The sprite lens is only needed for true Noir
        // grayscale and Dishwasher Dream blur. Applying it to every tinted/glowy filter is unnecessary
        // and can interfere with animated SpriteRenderer objects that swap sprites every frame.
        return currentPreset == FilterPreset.Noir || currentPreset == FilterPreset.DishwasherDream;
    }

    private void ApplySpriteRendererLens()
    {
        if (!ShouldUseSpriteRendererLens())
        {
            RestoreSpriteRendererLensMaterials();
            return;
        }

        EnsureSpriteLensMaterial();
        if (spriteLensMaterial == null)
        {
            return;
        }

        SpriteRenderer[] renderers = includeInactiveSpriteRenderers
            ? Resources.FindObjectsOfTypeAll<SpriteRenderer>()
            : FindObjectsOfType<SpriteRenderer>();

        HashSet<SpriteRenderer> currentlySeen = new HashSet<SpriteRenderer>();

        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer == null || !spriteRenderer.gameObject.scene.IsValid())
            {
                continue;
            }

            if (!includeInactiveSpriteRenderers && !spriteRenderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            currentlySeen.Add(spriteRenderer);

            if (!originalSpriteRendererMaterials.ContainsKey(spriteRenderer))
            {
                originalSpriteRendererMaterials.Add(spriteRenderer, spriteRenderer.sharedMaterial);
            }

            Material rendererLensMaterial;
            if (!activeSpriteRendererLensMaterials.TryGetValue(spriteRenderer, out rendererLensMaterial) || rendererLensMaterial == null)
            {
                rendererLensMaterial = new Material(spriteLensMaterial)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                activeSpriteRendererLensMaterials[spriteRenderer] = rendererLensMaterial;
            }

            ApplyLensSettingsToMaterial(rendererLensMaterial);

            // Use a per-renderer material instance. Reusing one shared sprite lens material across every SpriteRenderer
            // can make animated sprites fight over _MainTex, which shows up as duck/bubble sprite swapping.
            if (spriteRenderer.sharedMaterial != rendererLensMaterial)
            {
                spriteRenderer.material = rendererLensMaterial;
            }
        }

        RemoveMissingSpriteRendererLensEntries(currentlySeen);
    }

    private void ApplyLensSettingsToMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        material.SetFloat("_Grayscale", Active.grayscaleStrength);
        material.SetFloat("_Saturation", Active.saturation);
        material.SetFloat("_Contrast", Active.contrast);
        material.SetFloat("_Brightness", Active.brightness);
        material.SetColor("_TintColor", Active.tintColor);
        material.SetFloat("_TintStrength", Active.tintStrength);
        material.SetFloat("_BlurStrength", Active.blurStrength);
        material.SetFloat("_GlowStrength", Active.glowStrength);
        material.SetColor("_GlowColor", Active.glowColor);
        material.SetFloat("_HazeStrength", Active.hazeStrength);
        material.SetColor("_HazeColor", Active.hazeColor);
    }

    private void RestoreSpriteRendererLensMaterials()
    {
        if (originalSpriteRendererMaterials.Count == 0 && activeSpriteRendererLensMaterials.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<SpriteRenderer, Material> entry in originalSpriteRendererMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.sharedMaterial = entry.Value;
            }
        }

        foreach (KeyValuePair<SpriteRenderer, Material> entry in activeSpriteRendererLensMaterials)
        {
            if (entry.Value != null)
            {
                if (Application.isPlaying)
                    Destroy(entry.Value);
                else
                    DestroyImmediate(entry.Value);
            }
        }

        originalSpriteRendererMaterials.Clear();
        activeSpriteRendererLensMaterials.Clear();
    }

    private void RemoveMissingSpriteRendererLensEntries(HashSet<SpriteRenderer> currentlySeen)
    {
        if (originalSpriteRendererMaterials.Count == 0 && activeSpriteRendererLensMaterials.Count == 0)
        {
            return;
        }

        List<SpriteRenderer> toRemove = new List<SpriteRenderer>();
        foreach (KeyValuePair<SpriteRenderer, Material> entry in originalSpriteRendererMaterials)
        {
            if (entry.Key == null || !currentlySeen.Contains(entry.Key))
            {
                if (entry.Key != null)
                {
                    entry.Key.sharedMaterial = entry.Value;
                }

                toRemove.Add(entry.Key);
            }
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            SpriteRenderer spriteRenderer = toRemove[i];
            originalSpriteRendererMaterials.Remove(spriteRenderer);

            Material lensMaterial;
            if (activeSpriteRendererLensMaterials.TryGetValue(spriteRenderer, out lensMaterial))
            {
                if (lensMaterial != null)
                {
                    if (Application.isPlaying)
                        Destroy(lensMaterial);
                    else
                        DestroyImmediate(lensMaterial);
                }

                activeSpriteRendererLensMaterials.Remove(spriteRenderer);
            }
        }
    }

    private void RestoreUiImageLensMaterials()
    {
        if (originalUiImageMaterials.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Graphic, Material> entry in originalUiImageMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.material = entry.Value;
            }
        }

        originalUiImageMaterials.Clear();
    }

    private void RemoveMissingUiImageLensEntries()
    {
        if (originalUiImageMaterials.Count == 0)
        {
            return;
        }

        List<Graphic> toRemove = null;
        foreach (KeyValuePair<Graphic, Material> entry in originalUiImageMaterials)
        {
            if (entry.Key == null)
            {
                if (toRemove == null)
                {
                    toRemove = new List<Graphic>();
                }

                toRemove.Add(entry.Key);
            }
        }

        if (toRemove == null)
        {
            return;
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            originalUiImageMaterials.Remove(toRemove[i]);
        }
    }

    private void EnsureCameraEffect()
    {
        if (cameraEffect != null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        cameraEffect = targetCamera.GetComponent<GameFilterCameraEffect>();

        if (cameraEffect == null && autoAddCameraEffect)
        {
            cameraEffect = targetCamera.gameObject.AddComponent<GameFilterCameraEffect>();
        }
    }

    private void ApplyFilterState()
    {
        if (canvas != null)
        {
            canvas.enabled = filterEnabled;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = filterEnabled ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void ApplyPreset(FilterPreset preset)
    {
        currentPreset = preset;
        RefreshActiveSettingsFromPreset();

        if (Application.isPlaying)
        {
            BuildGeneratedUIIfNeeded();
            RebuildTextures();
            ApplyFilterState();
            ApplyCameraPostProcess();
        }

        if (savePresetPreference)
        {
            PlayerPrefs.SetInt(presetPrefsKey, (int)currentPreset);
            PlayerPrefs.Save();
        }
    }

    public void ApplyDirtyGreasePreset() => ApplyPreset(FilterPreset.DirtyGrease);
    public void ApplySubtleNeonPreset() => ApplyPreset(FilterPreset.SubtleNeon);
    public void ApplyPulsingVioletCRTPreset() => ApplyPreset(FilterPreset.PulsingVioletCRT);
    public void ApplyCleanPreset() => ApplyPreset(FilterPreset.Clean);
    public void ApplyNoirPreset() => ApplyPreset(FilterPreset.Noir);
    public void ApplyLateShiftNeonPreset() => ApplyPreset(FilterPreset.LateShiftNeon);
    public void ApplyHeatLampPreset() => ApplyPreset(FilterPreset.HeatLamp);
    public void ApplySteamFogPreset() => ApplyPreset(FilterPreset.SteamFog);
    public void ApplyArcadeScanlinesPreset() => ApplyPreset(FilterPreset.ArcadeScanlines);
    public void ApplyDishwasherDreamPreset() => ApplyPreset(FilterPreset.DishwasherDream);
    public void ApplyStaticColorPreset() => ApplyPreset(FilterPreset.StaticColor);
    public void ApplyBalatroStylePreset() => ApplyPulsingVioletCRTPreset();

    public FilterPreset GetCurrentPreset()
    {
        return currentPreset;
    }

    public void SetFilterEnabled(bool enabled)
    {
        filterEnabled = enabled;
        ApplyFilterState();
        ApplyCameraPostProcess();

        if (saveEnabledPreference)
        {
            PlayerPrefs.SetInt(enabledPrefsKey, filterEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public void ToggleFilter()
    {
        SetFilterEnabled(!filterEnabled);
    }

    public bool IsFilterEnabled()
    {
        return filterEnabled;
    }

    public void SetStaticColor(Color color)
    {
        staticColor.colorWash = color;
        staticColor.proceduralMode = ProceduralLensMode.Off;
        staticColor.useCameraPostProcess = false;

        if (currentPreset == FilterPreset.StaticColor)
        {
            RefreshActiveSettingsFromPreset();

            if (Application.isPlaying)
            {
                RebuildTextures();
                ApplyFilterState();
                ApplyCameraPostProcess();
            }
        }
    }

    public void SetStaticColorFromRGBA(float red, float green, float blue, float alpha)
    {
        SetStaticColor(new Color(Mathf.Clamp01(red), Mathf.Clamp01(green), Mathf.Clamp01(blue), Mathf.Clamp01(alpha)));
    }

    public void SetStaticColorFromHtml(string htmlColor)
    {
        if (string.IsNullOrWhiteSpace(htmlColor))
        {
            return;
        }

        if (!htmlColor.StartsWith("#"))
        {
            htmlColor = "#" + htmlColor;
        }

        if (ColorUtility.TryParseHtmlString(htmlColor, out Color parsed))
        {
            SetStaticColor(parsed);
        }
    }

    private void DestroyTexture(Texture2D texture)
    {
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
            Destroy(texture);
        else
            DestroyImmediate(texture);
    }

    private float PseudoRandom01(int index, float salt)
    {
        return Mathf.Repeat(Mathf.Sin(index * 12.9898f + salt * 78.233f) * 43758.5453f, 1f);
    }

    private static FilterSettings CreateCleanDefaults()
    {
        return new FilterSettings();
    }

    private static FilterSettings CreateStaticColorDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0f, 0f, 0f, 0.30f),
            useCameraPostProcess = false
        };
    }

    private static FilterSettings CreateDirtyGreaseDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0.28f, 0.13f, 0.030f, 0.055f),
            colorPulseStrength = 0.004f,
            colorPulseSpeed = 0.12f,
            proceduralMode = ProceduralLensMode.GreaseCornerBlobs,
            proceduralColorA = new Color(0.58f, 0.25f, 0.055f, 1f),
            proceduralColorB = new Color(0.18f, 0.075f, 0.010f, 1f),
            proceduralAlpha = 0.78f,
            proceduralScale = 1f,
            proceduralSpeed = 0.10f,
            proceduralIntensity = 1.35f,
            proceduralDirection = new Vector2(0.10f, -0.05f),
            useWaterSpots = true,
            waterSpotColor = new Color(0.62f, 0.43f, 0.20f, 1f),
            waterSpotAlpha = 0.10f,
            waterSpotCount = 20,
            waterSpotMinRadius = 0.035f,
            waterSpotMaxRadius = 0.12f,
            waterSpotRingThickness = 0.020f,
            useStreaks = true,
            streakColor = new Color(0.35f, 0.16f, 0.035f, 1f),
            streakAlpha = 0.11f,
            streakCount = 16,
            streakWidth = 0.030f,
            streakLength = 0.80f,
            streakAngle = -8f,
            streakDriftSpeed = new Vector2(0.001f, 0.002f),
            scanlineAlpha = 0f,
            noiseAlpha = 0.004f,
            noiseRefreshesPerSecond = 1.5f,
            vignetteColor = new Color(0.16f, 0.070f, 0.016f, 1f),
            vignetteAlpha = 0.22f,
            vignetteInnerRadius = 0.30f,
            useCameraPostProcess = true,
            saturation = 0.95f,
            contrast = 1.02f,
            brightness = 0.00f,
            tintColor = new Color(1.0f, 0.75f, 0.45f, 1f),
            tintStrength = 0.025f
        };
    }

    private static FilterSettings CreateSubtleNeonDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0.00f, 0.12f, 0.12f, 0.050f),
            proceduralMode = ProceduralLensMode.NeonGlow,
            proceduralColorA = new Color(0.00f, 0.82f, 0.74f, 1f),
            proceduralColorB = new Color(0.92f, 0.08f, 0.68f, 1f),
            proceduralAlpha = 0.28f,
            proceduralScale = 1.00f,
            proceduralSpeed = 0.45f,
            proceduralIntensity = 0.70f,
            scanlineAlpha = 0.008f,
            noiseAlpha = 0.002f,
            vignetteColor = new Color(0.00f, 0.015f, 0.030f, 1f),
            vignetteAlpha = 0.07f,
            vignetteInnerRadius = 0.55f,
            useCameraPostProcess = true,
            saturation = 1.08f,
            contrast = 1.03f,
            brightness = 0.006f,
            tintColor = new Color(0.55f, 1.0f, 0.95f, 1f),
            tintStrength = 0.018f,
            blurStrength = 0.08f,
            glowStrength = 0.22f,
            glowColor = new Color(0.20f, 1f, 0.92f, 1f),
            hazeStrength = 0.035f,
            hazeColor = new Color(0.40f, 0.95f, 1f, 1f)
        };
    }

    private static FilterSettings CreateLateShiftNeonDefaults()
    {
        return new FilterSettings
        {
            // A darker cyan-to-pink color wash sits underneath the moving glow spots.
            colorWash = new Color(0.000f, 0.055f, 0.105f, 0.125f),
            pulseColorWashTowardTarget = true,
            colorWashPulseTarget = new Color(0.120f, 0.000f, 0.105f, 0.125f),
            colorPulseStrength = 0.70f,
            colorPulseSpeed = 0.070f,
            useOverlayGlow = true,
            overlayGlowColorA = new Color(0.00f, 0.25f, 0.36f, 1f),
            overlayGlowColorB = new Color(0.34f, 0.00f, 0.22f, 1f),
            overlayGlowAlpha = 0.040f,
            overlayGlowPulseStrength = 0.012f,
            overlayGlowPulseSpeed = 0.18f,
            overlayGlowFlickerStrength = 0.006f,
            overlayGlowFlickerSpeed = 0.9f,
            proceduralMode = ProceduralLensMode.NeonGlow,
            proceduralColorA = new Color(0.00f, 0.70f, 0.88f, 1f),
            proceduralColorB = new Color(0.72f, 0.04f, 0.52f, 1f),
            proceduralAlpha = 0.30f,
            proceduralScale = 1.05f,
            proceduralSpeed = 0.62f,
            proceduralIntensity = 0.78f,
            scanlineAlpha = 0.010f,
            noiseAlpha = 0.004f,
            vignetteColor = new Color(0.000f, 0.014f, 0.036f, 1f),
            vignetteAlpha = 0.11f,
            vignetteInnerRadius = 0.48f,
            useCameraPostProcess = true,
            saturation = 1.18f,
            contrast = 1.06f,
            brightness = 0.012f,
            tintColor = new Color(0.55f, 1.0f, 0.95f, 1f),
            tintStrength = 0.024f,
            blurStrength = 0.10f,
            glowStrength = 0.31f,
            glowColor = new Color(0.10f, 0.95f, 0.90f, 1f),
            hazeStrength = 0.050f,
            hazeColor = new Color(0.25f, 0.95f, 1f, 1f)
        };
    }

    private static FilterSettings CreatePulsingVioletCRTDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0.12f, 0.035f, 0.26f, 0.20f),
            colorPulseStrength = 0.045f,
            colorPulseSpeed = 0.80f,
            proceduralMode = ProceduralLensMode.FixedTriColorNeonGlow,
            proceduralColorA = new Color(0.00f, 0.36f, 1.00f, 1f), // blue, bottom left
            proceduralColorB = new Color(1.00f, 0.10f, 0.72f, 1f), // pink, bottom right
            proceduralAlpha = 0.30f,
            proceduralSpeed = 0.95f,
            proceduralIntensity = 0.95f,
            scanlineAlpha = 0.11f,
            scanlineSpacingPixels = 4,
            scanlineScrollSpeed = 0.07f,
            noiseAlpha = 0.045f,
            noiseRefreshesPerSecond = 8f,
            noiseDriftSpeed = 0.030f,
            vignetteColor = new Color(0.01f, 0f, 0.04f, 1f),
            vignetteAlpha = 0.25f,
            vignetteInnerRadius = 0.38f,
            useCameraPostProcess = true,
            saturation = 1.15f,
            contrast = 1.12f,
            blurStrength = 0.04f,
            glowStrength = 0.20f,
            glowColor = new Color(0.78f, 0.25f, 1f, 1f)
        };
    }

    private static FilterSettings CreateNoirDefaults()
    {
        return new FilterSettings
        {
            // Keep Noir stripped down to true black-and-white only.
            // Previous old-film extras are intentionally left out for now because they made the background too dark.
            // If we want to return to that look later, restore: film scratches/streaks, scanlines, grain, and vignette.
            colorWash = new Color(0f, 0f, 0f, 0f),
            proceduralMode = ProceduralLensMode.Off,
            useStreaks = false,
            streakAlpha = 0f,
            scanlineAlpha = 0f,
            noiseAlpha = 0f,
            vignetteAlpha = 0f,
            useCameraPostProcess = true,
            grayscaleStrength = 1f,
            saturation = 0f,
            contrast = 1.06f,
            brightness = 0.020f,
            tintStrength = 0f,
            blurStrength = 0f,
            glowStrength = 0f,
            hazeStrength = 0f
        };
    }

    private static FilterSettings CreateHeatLampDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0.50f, 0.105f, 0.010f, 0.105f),
            colorPulseStrength = 0.026f,
            colorPulseSpeed = 0.30f,
            useOverlayGlow = true,
            overlayGlowColorA = new Color(0.65f, 0.15f, 0.02f, 1f),
            overlayGlowColorB = new Color(0.38f, 0.05f, 0.00f, 1f),
            overlayGlowAlpha = 0.046f,
            overlayGlowPulseStrength = 0.024f,
            overlayGlowPulseSpeed = 0.34f,
            overlayGlowFlickerStrength = 0.014f,
            overlayGlowFlickerSpeed = 0.9f,
            proceduralMode = ProceduralLensMode.LavaLampBlobs,
            proceduralColorA = new Color(1.00f, 0.34f, 0.02f, 1f),
            proceduralColorB = new Color(0.95f, 0.05f, 0.00f, 1f),
            proceduralAlpha = 0.44f,
            proceduralScale = 1.00f,
            proceduralSpeed = 0.34f,
            proceduralIntensity = 1.18f,
            proceduralDirection = new Vector2(0f, 1f),
            noiseAlpha = 0.003f,
            vignetteColor = new Color(0.25f, 0.035f, 0.00f, 1f),
            vignetteAlpha = 0.18f,
            vignetteInnerRadius = 0.40f,
            useCameraPostProcess = true,
            saturation = 1.13f,
            contrast = 1.04f,
            brightness = 0.008f,
            tintColor = new Color(1f, 0.56f, 0.28f, 1f),
            tintStrength = 0.044f,
            blurStrength = 0.035f,
            glowStrength = 0.15f,
            glowColor = new Color(1f, 0.36f, 0.08f, 1f),
            hazeStrength = 0.030f,
            hazeColor = new Color(1f, 0.45f, 0.20f, 1f)
        };
    }

    private static FilterSettings CreateSteamFogDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0.70f, 0.88f, 0.90f, 0.085f),
            colorPulseStrength = 0.010f,
            colorPulseSpeed = 0.12f,
            proceduralMode = ProceduralLensMode.RollingFogLines,
            proceduralColorA = new Color(0.86f, 0.96f, 0.96f, 1f),
            proceduralColorB = new Color(0.60f, 0.75f, 0.76f, 1f),
            proceduralAlpha = 0.58f,
            proceduralScale = 1.0f,
            proceduralSpeed = 0.56f,
            proceduralIntensity = 1.10f,
            proceduralDirection = new Vector2(-1f, 0f),
            useWaterSpots = true,
            waterSpotColor = new Color(0.86f, 0.95f, 0.95f, 1f),
            waterSpotAlpha = 0.035f,
            waterSpotCount = 10,
            useStreaks = false,
            noiseAlpha = 0.006f,
            noiseRefreshesPerSecond = 1.2f,
            vignetteColor = new Color(0.45f, 0.58f, 0.58f, 1f),
            vignetteAlpha = 0.08f,
            vignetteInnerRadius = 0.60f,
            useCameraPostProcess = true,
            saturation = 0.93f,
            contrast = 0.94f,
            brightness = 0.018f,
            tintColor = new Color(0.82f, 0.98f, 1f, 1f),
            tintStrength = 0.030f,
            blurStrength = 0.22f,
            glowStrength = 0.06f,
            glowColor = new Color(0.80f, 0.95f, 1f, 1f),
            hazeStrength = 0.34f,
            hazeColor = new Color(0.82f, 0.95f, 0.96f, 1f)
        };
    }

    private static FilterSettings CreateArcadeScanlinesDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0f, 0.04f, 0.13f, 0.19f),
            colorPulseStrength = 0.045f,
            colorPulseSpeed = 0.75f,
            proceduralMode = ProceduralLensMode.FixedTriColorNeonGlow,
            proceduralColorA = new Color(0.00f, 0.40f, 1.00f, 1f), // blue, bottom left
            proceduralColorB = new Color(1.00f, 0.08f, 0.74f, 1f), // pink, bottom right
            proceduralAlpha = 0.22f,
            proceduralSpeed = 1.10f,
            proceduralIntensity = 1.15f,
            scanlineAlpha = 0.34f,
            scanlineSpacingPixels = 3,
            scanlineScrollSpeed = 0.18f,
            noiseAlpha = 0.105f,
            noiseRefreshesPerSecond = 12f,
            noiseDriftSpeed = 0.040f,
            vignetteColor = new Color(0f, 0f, 0.05f, 1f),
            vignetteAlpha = 0.26f,
            vignetteInnerRadius = 0.36f,
            useCameraPostProcess = true,
            saturation = 1.25f,
            contrast = 1.26f,
            brightness = -0.010f,
            blurStrength = 0.02f,
            glowStrength = 0.14f,
            glowColor = new Color(0.20f, 0.80f, 1f, 1f)
        };
    }

    private static FilterSettings CreateDishwasherDreamDefaults()
    {
        return new FilterSettings
        {
            colorWash = new Color(0.18f, 0.42f, 0.70f, 0.048f),
            colorPulseStrength = 0.006f,
            colorPulseSpeed = 0.18f,
            proceduralMode = ProceduralLensMode.DreamHaze,
            proceduralColorA = new Color(0.62f, 0.90f, 1.00f, 1f),
            proceduralColorB = new Color(0.95f, 1.00f, 1.00f, 1f),
            proceduralAlpha = 0.16f,
            proceduralScale = 1.0f,
            proceduralSpeed = 0.20f,
            proceduralIntensity = 0.42f,
            useWaterSpots = true,
            waterSpotColor = new Color(0.72f, 0.92f, 1.00f, 1f),
            waterSpotAlpha = 0.020f,
            waterSpotCount = 14,
            waterSpotMinRadius = 0.045f,
            waterSpotMaxRadius = 0.18f,
            waterSpotRingThickness = 0.018f,
            noiseAlpha = 0.002f,
            vignetteColor = new Color(0.00f, 0.05f, 0.10f, 1f),
            vignetteAlpha = 0.035f,
            vignetteInnerRadius = 0.62f,
            useCameraPostProcess = true,
            saturation = 1.00f,
            contrast = 0.92f,
            brightness = 0.010f,
            tintColor = new Color(0.70f, 0.92f, 1f, 1f),
            tintStrength = 0.020f,
            blurStrength = 0.58f,
            glowStrength = 0.08f,
            glowColor = new Color(0.55f, 0.85f, 1f, 1f),
            hazeStrength = 0.14f,
            hazeColor = new Color(0.76f, 0.94f, 1f, 1f)
        };
    }
}
