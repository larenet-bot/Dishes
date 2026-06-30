using UnityEngine;
using UnityEngine.Audio;

public class AudioInitializer : MonoBehaviour
{
    public AudioMixer audioMixer;

    void Awake()
    {
        // Load saved settings or use 0.5f (50%) as default
        float master = PlayerPrefs.GetFloat("Master", 0.5f);
        float music = PlayerPrefs.GetFloat("Music", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        SetVolume("Master", master);
        SetVolume("Music", music);
        SetVolume("SFX", sfx);

        // Ensure settings are saved
        PlayerPrefs.SetFloat("Master", master);
        PlayerPrefs.SetFloat("Music", music);
        PlayerPrefs.SetFloat("SFX", sfx);
        PlayerPrefs.Save();
    }

    private void SetVolume(string paramName, float value)
    {
        if (audioMixer == null) return;

        if (value <= 0.0001f)
            audioMixer.SetFloat(paramName, -80f);
        else
            audioMixer.SetFloat(paramName, Mathf.Log10(value) * 20);
    }
}
