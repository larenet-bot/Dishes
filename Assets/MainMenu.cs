using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{ //defines functions of main menu
    public void Start() // plays main theme sound at start of game
    {
        // Updated to use FindFirstObjectByType as per the deprecation warning
        //Object.FindFirstObjectByType<AudioManager>().Play("menuMusic");
    }
    public void PlayGame() // opens next scene in queue 
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()  //quits game. DOES NOT SHOW IN UNITY EDITOR, if debug log reads "QUIT!", the function worked
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }
}
