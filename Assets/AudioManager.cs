using UnityEngine;
using UnityEngine.Audio;
using System;

public class AudioManager : MonoBehaviour
{
    public Sounds[] sounds; // Array of Sounds objects

    public static AudioManager instance; // Singleton instance

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance == null)
        {
            instance = this; // Set the singleton instance to this AudioManager
        }
        else
        {
            Destroy(gameObject); // If an instance already exists, destroy this one
            return; // Exit the Awake method to prevent further execution
        }

        DontDestroyOnLoad(gameObject);
        foreach (Sounds s in sounds)
        {
            gameObject.AddComponent<AudioSource>(); // Add AudioSource component to the GameObject
            s.source = gameObject.GetComponent<AudioSource>(); // Get the AudioSource component
            s.source.clip = s.clip;

            s.source.volume = s.volume; // Set the volume
            s.source.pitch = s.pitch; // Set the pitch
            s.source.loop = s.loop; // Set the loop property
        }

        // If another AudioManager already exists, destroy this one
        
    }

    private void Start()
    {
        Play("MainTheme"); // Play the main theme sound at the start
    }

    // Update is called once per frame
    public void Play(string name)
    {
        Sounds s = System.Array.Find(sounds, sound => sound.name == name);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + name + " not found!");
            return;
        }
        s.source.Play();
    }
}
