using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class SettingsMenu : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown resolutionDropdown;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    public AudioMixer audioMixer;

    public Toggle duckAlternateToggle;
    public Toggle tooltipToggle;

    [Header("Visual Filter")]
    [SerializeField] private GameFilterOverlay gameFilterOverlay;
    public Toggle visualFilterToggle;

    [Header("World Objects Disabled During Settings")]
    [SerializeField, Tooltip("Scene objects placed here will have their Collider2D disabled while the settings menu is open. Use this for the duck and other fixed clickable objects.")]
    private GameObject[] objectsToDisableDuringSettings;

    [SerializeField, Tooltip("Also disables every active Collider2D on this layer while settings are open. Layer 6 is your bubbles layer.")]
    private int bubbleLayer = 6;

    [SerializeField, Tooltip("How often to check for newly spawned bubbles while settings are open.")]
    private float settingsColliderRefreshInterval = 0.1f;

    private readonly List<Collider2D> collidersDisabledBySettings = new List<Collider2D>();
    private float settingsColliderRefreshTimer = 0f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        settingsColliderRefreshTimer = 0f;
        DisableSettingsObjectColliders(true);
        SyncVisualFilterToggle();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        RestoreSettingsObjectColliders();
    }

    private void Start()
    {
        Debug.Log("SettingsMenu started. Current resolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + " fullscreen=" + Screen.fullScreen);

        if (resolutionDropdown != null)
        {
            resolutionDropdown.gameObject.SetActive(false);
        }

        if (duckAlternateToggle != null)
        {
            bool useAlt = PlayerPrefs.GetInt("DuckAlternate", 0) == 1;
            duckAlternateToggle.isOn = useAlt;
            duckAlternateToggle.onValueChanged.AddListener(SetDuckAlternate);
        }

        if (tooltipToggle != null)
        {
            bool tooltipsOn = PlayerPrefs.GetInt("TooltipsEnabled", 1) == 1;
            tooltipToggle.isOn = tooltipsOn;
            tooltipToggle.onValueChanged.AddListener(SetTooltipsEnabled);
            // apply initial state immediately
            SetTooltipsEnabled(tooltipsOn);
        }

        BindVisualFilterToggle();

        float master = PlayerPrefs.GetFloat("Master", 0.5f);
        float music = PlayerPrefs.GetFloat("Music", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("Master", master <= 0.0001f ? -80f : Mathf.Log10(master) * 20);
            audioMixer.SetFloat("Music", music <= 0.0001f ? -80f : Mathf.Log10(music) * 20);
            audioMixer.SetFloat("SFX", sfx <= 0.0001f ? -80f : Mathf.Log10(sfx) * 20);
        }

        if (masterSlider != null)
            masterSlider.value = master;

        if (musicSlider != null)
            musicSlider.value = music;

        if (sfxSlider != null)
            sfxSlider.value = sfx;

        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }

    private void Update()
    {
        settingsColliderRefreshTimer += Time.unscaledDeltaTime;

        if (settingsColliderRefreshTimer >= settingsColliderRefreshInterval)
        {
            settingsColliderRefreshTimer = 0f;
            DisableSettingsObjectColliders(false);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        Debug.Log("Resolution change attempted but is locked to 1920 x 1080.");
    }

    public void SetMasterVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("Master", volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20);

        PlayerPrefs.SetFloat("Master", volume);
    }

    public void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("Music", volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20);

        PlayerPrefs.SetFloat("Music", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
            audioMixer.SetFloat("SFX", volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20);

        PlayerPrefs.SetFloat("SFX", volume);
    }

    public void SetDuckAlternate(bool isAlternate)
    {
        PlayerPrefs.SetInt("DuckAlternate", isAlternate ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log("Duck alternate audio set to: " + isAlternate);
    }

    public void SetTooltipsEnabled(bool isEnabled)
    {
        PlayerPrefs.SetInt("TooltipsEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save();

        TooltipManager[] managers = Resources.FindObjectsOfTypeAll<TooltipManager>();

        for (int i = 0; i < managers.Length; i++)
        {
            TooltipManager mgr = managers[i];
            if (mgr == null)
                continue;

            if (mgr.gameObject.scene.IsValid())
            {
                mgr.enabled = isEnabled;
            }
        }

        Debug.Log("Tooltips enabled set to: " + isEnabled);
    }

    private void BindVisualFilterToggle()
    {
        CacheVisualFilterOverlay();

        if (visualFilterToggle == null)
            return;

        visualFilterToggle.onValueChanged.RemoveListener(SetVisualFilterEnabled);
        SyncVisualFilterToggle();
        visualFilterToggle.onValueChanged.AddListener(SetVisualFilterEnabled);
    }

    private void SyncVisualFilterToggle()
    {
        CacheVisualFilterOverlay();

        if (visualFilterToggle == null)
            return;

        bool filterIsEnabled = gameFilterOverlay != null && gameFilterOverlay.IsFilterEnabled();
        visualFilterToggle.SetIsOnWithoutNotify(filterIsEnabled);
    }

    public void SetVisualFilterEnabled(bool isEnabled)
    {
        CacheVisualFilterOverlay();

        if (gameFilterOverlay != null)
        {
            gameFilterOverlay.SetFilterEnabled(isEnabled);
        }
        else
        {
            PlayerPrefs.SetInt("GameFilterOverlayEnabled", isEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (visualFilterToggle != null)
        {
            visualFilterToggle.SetIsOnWithoutNotify(isEnabled);
        }

        Debug.Log("Visual filter enabled set to: " + isEnabled);
    }

    public void ToggleVisualFilter()
    {
        CacheVisualFilterOverlay();

        bool nextState = true;

        if (gameFilterOverlay != null)
        {
            nextState = !gameFilterOverlay.IsFilterEnabled();
        }
        else if (visualFilterToggle != null)
        {
            nextState = !visualFilterToggle.isOn;
        }

        SetVisualFilterEnabled(nextState);
    }

    private void CacheVisualFilterOverlay()
    {
        if (gameFilterOverlay != null)
            return;

        if (GameFilterOverlay.Instance != null)
        {
            gameFilterOverlay = GameFilterOverlay.Instance;
            return;
        }

        GameFilterOverlay[] overlays = Resources.FindObjectsOfTypeAll<GameFilterOverlay>();

        for (int i = 0; i < overlays.Length; i++)
        {
            GameFilterOverlay overlay = overlays[i];

            if (overlay == null)
                continue;

            if (!overlay.gameObject.scene.IsValid())
                continue;

            gameFilterOverlay = overlay;
            return;
        }
    }

    public void SetFullScreen(bool isFullScreen)
    {
        if (isFullScreen)
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
        }

        Debug.Log("Fullscreen mode set to: " + isFullScreen + " (mode: " + Screen.fullScreenMode + ")");
    }

    public void OpenSettings()
    {
        gameObject.SetActive(true);
    }

    public void CloseSettings()
    {
        gameObject.SetActive(false);
    }

    public void ToggleSettings()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void DisableSettingsObjectColliders(bool clearExistingList)
    {
        if (clearExistingList)
        {
            collidersDisabledBySettings.Clear();
        }

        DisableColliderArrayObjects();
        DisableCollidersOnLayer(bubbleLayer);
    }

    private void DisableColliderArrayObjects()
    {
        if (objectsToDisableDuringSettings == null)
            return;

        for (int i = 0; i < objectsToDisableDuringSettings.Length; i++)
        {
            GameObject obj = objectsToDisableDuringSettings[i];

            if (obj == null)
                continue;

            Collider2D[] colliders = obj.GetComponentsInChildren<Collider2D>(true);

            for (int c = 0; c < colliders.Length; c++)
            {
                DisableColliderIfEnabled(colliders[c]);
            }
        }
    }

    private void DisableCollidersOnLayer(int layer)
    {
        Collider2D[] allColliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);

        for (int i = 0; i < allColliders.Length; i++)
        {
            Collider2D collider = allColliders[i];

            if (collider == null)
                continue;

            if (!ColliderOrParentIsOnLayer(collider, layer))
                continue;

            DisableColliderIfEnabled(collider);
        }
    }

    private bool ColliderOrParentIsOnLayer(Collider2D collider, int layer)
    {
        if (collider == null)
            return false;

        Transform current = collider.transform;

        while (current != null)
        {
            if (current.gameObject.layer == layer)
                return true;

            current = current.parent;
        }

        return false;
    }

    private void DisableColliderIfEnabled(Collider2D collider)
    {
        if (collider == null)
            return;

        if (!collider.enabled)
            return;

        collider.enabled = false;

        if (!collidersDisabledBySettings.Contains(collider))
        {
            collidersDisabledBySettings.Add(collider);
        }
    }

    private void RestoreSettingsObjectColliders()
    {
        for (int i = 0; i < collidersDisabledBySettings.Count; i++)
        {
            if (collidersDisabledBySettings[i] != null)
            {
                collidersDisabledBySettings[i].enabled = true;
            }
        }

        collidersDisabledBySettings.Clear();
        settingsColliderRefreshTimer = 0f;
    }
}