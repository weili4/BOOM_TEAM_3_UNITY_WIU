using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private string firstLevelSceneName = "MainMenu";
    [SerializeField] private AudioClip quietBGM; 

    private void Start()
    {
        // PLAY QUIET MAIN MENU BGM
        if (quietBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(quietBGM, 0.4f);
        }
    }

    public void StartGame()
    {
        // load first level scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(firstLevelSceneName);
    }

    public void QuitGame()
    {
        // quit game application
        Debug.Log("quit game pressed");
        Application.Quit();
    }
}