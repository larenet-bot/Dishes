using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{ //defines functions of Pause Menu

    public void CloseMenu() // closes pause menu
    {
        SceneManager.LoadScene("Game");
    }

    public void QuitGame()  //quits game. DOES NOT SHOW IN UNITY EDITOR, if debug log reads "QUIT!", the function worked
    {
        Debug.Log("QUIT!");
        Application.Quit();
    }

}
