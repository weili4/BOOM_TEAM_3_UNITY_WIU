using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    [Header("scene to load on play")]
    [SerializeField] private string firstLevelSceneName = "Level 1";

    [Header("ui panels")]
    [SerializeField] private GameObject mainButtonsPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("main buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("settings sliders & back button")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button settingsBackBtn;

    [Header("audio clips")]
    [SerializeField] private AudioClip menuBGM;
    [SerializeField] private AudioClip buttonClickSFX;

    private void Awake()
    {
        // hook button listeners
        if (playButton != null) playButton.onClick.AddListener(StartGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (settingsBackBtn != null) settingsBackBtn.onClick.AddListener(CloseSettings);

        // hook audio sliders
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetMasterVolume(val));
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetBGMVolume(val));
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetSFXVolume(val));
    }

    private void Start()
    {
        Time.timeScale = 1f;

        // wipe old singletons so new game starts with fresh party and empty inventory
        ResetGameData();

        CloseSettings();

        if (menuBGM != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMusic(menuBGM, 0.4f);
        }
    }

    private void ResetGameData()
    {
        if (PartyManager.Instance != null)
        {
            Destroy(PartyManager.Instance.gameObject);
        }

        if (Inventory.Instance != null)
        {
            Destroy(Inventory.Instance.gameObject);
        }
    }

    public void StartGame()
    {
        PlayButtonSFX();
        Time.timeScale = 1f;

        // trigger smooth black screen fade transition into level 1
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(firstLevelSceneName);
        }
        else
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }
    }

    public void OpenSettings()
    {
        PlayButtonSFX();
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        PlayButtonSFX();
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        PlayButtonSFX();
        Debug.Log("quit game selected");
        Application.Quit();
    }

    private void PlayButtonSFX()
    {
        if (buttonClickSFX != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(buttonClickSFX, Vector3.zero, 0.8f);
        }
    }
}