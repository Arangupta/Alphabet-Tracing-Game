using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public AudioSource Background_MusicSource;         // General AudioSource for other sounds
    public AudioClip Background_Music;          // General AudioClip for other sounds
    public void StartGame()
    {
        SceneManager.LoadScene("Gamescene");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
   
}
