using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine.UI;

using TMPro; // add at the top



public class SettingsMenu : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    public AudioMixer audioMixer; // Reference to the AudioMixer for volume control
    //public Dropdown resolutionDropdown; // Dropdown UI element for resolutions
    Resolution[] resolutions; // Array to hold available screen resolutions
    private void Start()
    {
        resolutions = Screen.resolutions; // Get all available screen resolutions

        resolutionDropdown.ClearOptions(); // Clear existing options in the dropdown
        List<string> options = new List<string>(); // Create a list to hold resolution options
                                                   // 
        int currentResolutionIndex = 0; // Variable to track the current resolution index
        for (int i =0; i < resolutions.Length; i++)
        {
            // Add each resolution to the options list
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i; // Set the current resolution index if it matches the current screen resolution
            }
        }
        resolutionDropdown.AddOptions(options); // Add the options to the dropdown
        resolutionDropdown.value = currentResolutionIndex; // Set the dropdown value to the current resolution index
        resolutionDropdown.RefreshShownValue(); // Refresh the dropdown to show the current value
    }
    public void SetResolution(int resolutionIndex)
    {
        // Set the screen resolution based on the selected index
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log("Resolution set to: " + resolution.width + " x " + resolution.height);
    }
    public void SetVolume (float volume)
    {
        // Set the game's volume
       audioMixer.SetFloat("Volume", volume);
        Debug.Log("Volume set to: " + volume);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        // Set the game's fullscreen mode
        Screen.fullScreen = isFullScreen;
        Debug.Log("Fullscreen mode set to: " + isFullScreen);
    }


}
