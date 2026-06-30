using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsPersist : MonoBehaviour
{
    public static AudioSettingsPersist instance;

    public AudioMixer audioMixer;

    private void Awake()
    {
        // Ensure only one instance exists across all scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Load and apply saved audio settings immediately
        ApplyAudioSettings();
    }

    private void ApplyAudioSettings()
    {
        if (audioMixer == null)
        {
            Debug.LogError("[AudioSettingsPersist] Audio mixer is not assigned!");
            return;
        }

        float master = PlayerPrefs.GetFloat("Master", 0.5f);
        float music = PlayerPrefs.GetFloat("Music", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        SetVolume("Master", master);
        SetVolume("Music", music);
        SetVolume("SFX", sfx);

        Debug.Log($"[AudioSettingsPersist] Audio settings applied - Master: {master}, Music: {music}, SFX: {sfx}");
    }

    private void SetVolume(string paramName, float value)
    {
        if (audioMixer == null) return;

        float dbValue = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20;
        audioMixer.SetFloat(paramName, dbValue);
    }

    public void UpdateAndSaveVolume(string paramName, float value)
    {
        SetVolume(paramName, value);
        PlayerPrefs.SetFloat(paramName, value);
        PlayerPrefs.Save();
    }
}