using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Scene Names")]
    public string introSceneName = "OpenScene";
    public string mainGameSceneName = "Game";

    public void Start()
    {
        
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(introSceneName);

        //// Check if player has seen the intro before
        //if (PlayerPrefs.GetInt("HasSeenIntro", 0) == 0)
        //{
        //    // First time playing — go to the intro cutscene
        //    PlayerPrefs.SetInt("HasSeenIntro", 1);
        //    PlayerPrefs.Save();

        //    SceneManager.LoadScene(introSceneName);
        //}
        //else
        //{
        //    // Skip straight to the main game
        //    SceneManager.LoadScene(mainGameSceneName);
        //}
    }

    public void QuitGame()
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }
}
