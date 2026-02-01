using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsLoader : MonoBehaviour
{
    public AudioMixer audioMixer;

    void Start()
    {
        ApplySavedSettings();
    }

    public void ApplySavedSettings()
    {
        float master = PlayerPrefs.GetFloat("Master", 0.5f);
        float music = PlayerPrefs.GetFloat("Music", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        // Apply to mixer (exposed parameter names must match: "Master", "Music", "SFX")
        if (audioMixer != null)
        {
            audioMixer.SetFloat(
                "Master",
                master <= 0.0001f ? -80f : Mathf.Log10(master) * 20
            );

            audioMixer.SetFloat(
                "Music",
                music <= 0.0001f ? -80f : Mathf.Log10(music) * 20
            );

            audioMixer.SetFloat(
                "SFX",
                sfx <= 0.0001f ? -80f : Mathf.Log10(sfx) * 20
            );
        }

        
    }
}
