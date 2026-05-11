using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.UI;

using TMPro; // add at the top



public class SettingsMenu : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    public AudioMixer audioMixer; // Reference to the AudioMixer for volume control

    // New: toggle to choose alternate duck audio list
    public Toggle duckAlternateToggle;

    // Removed the dynamic resolution list and dropdown population.
    private void Start()
    {
        // Do NOT force the window size every time the menu starts.
        // Previously this call caused the window to snap back to 1920x1080 when the menu opened.
        // If you want to lock resolution only on first run, use a PlayerPrefs flag (example below).
        // Example single-run lock (optional):
        // if (!PlayerPrefs.HasKey("ResolutionInitialized"))
        // {
        //     Screen.SetResolution(1920, 1080, Screen.fullScreen);
        //     PlayerPrefs.SetInt("ResolutionInitialized", 1);
        //     Debug.Log("Initial resolution locked to: 1920 x 1080");
        // }

        Debug.Log("SettingsMenu started. Current resolution: " + Screen.currentResolution.width + "x" + Screen.currentResolution.height + " fullscreen=" + Screen.fullScreen);

        // Hide or disable the resolution dropdown if assigned so user cannot change it from UI
        if (resolutionDropdown != null)
        {
            // Either deactivate the whole GameObject:
            resolutionDropdown.gameObject.SetActive(false);
            // Or just make it non-interactable:
            // resolutionDropdown.interactable = false;
        }

        // Initialize duck alternate toggle from PlayerPrefs and wire listener
        if (duckAlternateToggle != null)
        {
            bool useAlt = PlayerPrefs.GetInt("DuckAlternate", 0) == 1;
            duckAlternateToggle.isOn = useAlt;
            duckAlternateToggle.onValueChanged.AddListener(SetDuckAlternate);
        }

        // Load saved values or default to 0.5
        float master = PlayerPrefs.GetFloat("Master", 0.5f);
        float music = PlayerPrefs.GetFloat("Music", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        audioMixer.SetFloat("Master", master <= 0.0001f ? -80f : Mathf.Log10(master) * 20);
        audioMixer.SetFloat("Music", music <= 0.0001f ? -80f : Mathf.Log10(music) * 20);
        audioMixer.SetFloat("SFX", sfx <= 0.0001f ? -80f : Mathf.Log10(sfx) * 20);

        // Set sliders
        masterSlider.value = master;
        musicSlider.value = music;
        sfxSlider.value = sfx;

        // Apply to mixer
        SetMasterVolume(master);
        SetMusicVolume(music);
        SetSFXVolume(sfx);
    }

    // Kept as a no-op so any existing UI bindings won't change resolution.
    public void SetResolution(int resolutionIndex)
    {
        // Resolution change is locked. Do nothing.
        Debug.Log("Resolution change attempted but is locked to 1920 x 1080.");
    }

    // For a Master slider
    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("Master", volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Master", volume);
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("Music", volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("Music", volume);
    }

    public void SetSFXVolume(float volume)
    {
        audioMixer.SetFloat("SFX", volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFX", volume);
    }

    // New: store the duck audio preference
    public void SetDuckAlternate(bool isAlternate)
    {
        PlayerPrefs.SetInt("DuckAlternate", isAlternate ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("Duck alternate audio set to: " + isAlternate);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        // Use fullScreenMode to explicitly select mode, then set fullscreen on/off.
        // This avoids reapplying a fixed resolution and preserves windowed/maximized behavior.
        if (isFullScreen)
        {
            // You can choose FullScreenWindow (borderless window) or ExclusiveFullScreen
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.fullScreen = false;
            // Do NOT call Screen.SetResolution here — leave the window manager to keep the current size (e.g. maximized).
        }

        Debug.Log("Fullscreen mode set to: " + isFullScreen + " (mode: " + Screen.fullScreenMode + ")");
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
