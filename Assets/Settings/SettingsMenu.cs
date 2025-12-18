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

    // Removed the dynamic resolution list and dropdown population.
    private void Start()
    {
        // Force the game into the locked resolution immediately
        Screen.SetResolution(1920, 1080, Screen.fullScreen);
        Debug.Log("Resolution locked to: 1920 x 1080");

        // Hide or disable the resolution dropdown if assigned so user cannot change it from UI
        if (resolutionDropdown != null)
        {
            // Either deactivate the whole GameObject:
            resolutionDropdown.gameObject.SetActive(false);
            // Or just make it non-interactable:
            // resolutionDropdown.interactable = false;
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

    public void SetFullScreen(bool isFullScreen)
    {
        // Set the game's fullscreen mode
        Screen.fullScreen = isFullScreen;
        Debug.Log("Fullscreen mode set to: " + isFullScreen);

        // Re-apply locked resolution when fullscreen mode changes
        Screen.SetResolution(1920, 1080, Screen.fullScreen);
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
