using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour { //defines functions of main menu

    public void PlayGame () // opens next scene in queue 
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void QuitGame()  //quits game. DOES NOT SHOW IN UNITY EDITOR, if debug log reads "QUIT!", the function worked
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }

}
