using UnityEngine;
using UnityEngine.Audio;

public class AudioInitializer : MonoBehaviour
{
    public AudioMixer audioMixer;

    void Awake()
    {
        float master = PlayerPrefs.GetFloat("Master", 0.5f);
        float music = PlayerPrefs.GetFloat("Music", 0.5f);
        float sfx = PlayerPrefs.GetFloat("SFX", 0.5f);

        SetVolume("Master", master);
        SetVolume("Music", music);
        SetVolume("SFX", sfx);
    }

    private void SetVolume(string paramName, float value)
    {
        if (value <= 0.0001f)
            audioMixer.SetFloat(paramName, -80f);
        else
            audioMixer.SetFloat(paramName, Mathf.Log10(value) * 20);
    }
}
