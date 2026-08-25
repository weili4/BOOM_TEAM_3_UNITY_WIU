using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("root panel to show/hide")]
    [SerializeField] private GameObject pausePanelRoot;

    [Header("top tab buttons")]
    [SerializeField] private Button partyTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button settingsTabButton;

    [Header("tab sub-panels")]
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("settings sub-views")]
    [SerializeField] private GameObject settingsMainView;       // cheats, audio, main menu buttons
    [SerializeField] private GameObject cheatsSubView;          // revive all button + back
    [SerializeField] private GameObject audioSubView;           // 3 volume sliders + back
    [SerializeField] private GameObject mainMenuConfirmSubView; // are you sure prompt + yes/no

    [Header("settings buttons & sliders")]
    [SerializeField] private Button openCheatsBtn;
    [SerializeField] private Button openAudioBtn;
    [SerializeField] private Button openMainMenuConfirmBtn;

    [SerializeField] private Button cheatReviveAllBtn;
    [SerializeField] private Button cheatsBackBtn;
    [SerializeField] private Button cheatKillNearbyEnemiesBtn;
    [SerializeField] private float cheatKillRadius = 12.0f; // radius around the leader to wipe out enemies

    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button audioBackBtn;

    [SerializeField] private Button confirmMainMenuYesBtn;
    [SerializeField] private Button confirmMainMenuNoBtn;

    [Header("pause and ui audio clips")]
    [SerializeField] private AudioClip pauseOpenSFX;
    [SerializeField] private AudioClip tabSwitchSFX;
    [SerializeField] private AudioClip buttonClickSFX;

    [Header("scene names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int currentActiveTab = 2; // 0 = party, 1 = inventory, 2 = settings
    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (cheatKillNearbyEnemiesBtn != null) cheatKillNearbyEnemiesBtn.onClick.AddListener(ExecuteCheatKillNearbyEnemies);

        // top tab clicks
        if (partyTabButton != null) partyTabButton.onClick.AddListener(() => { PlayTabSFX(); SwitchTab(0); });
        if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(() => { PlayTabSFX(); SwitchTab(1); });
        if (settingsTabButton != null) settingsTabButton.onClick.AddListener(() => { PlayTabSFX(); SwitchTab(2); });

        // settings navigation buttons
        if (openCheatsBtn != null) openCheatsBtn.onClick.AddListener(() => { PlayButtonSFX(); OpenSettingsSubView(1); });
        if (openAudioBtn != null) openAudioBtn.onClick.AddListener(() => { PlayButtonSFX(); OpenSettingsSubView(2); });
        if (openMainMenuConfirmBtn != null) openMainMenuConfirmBtn.onClick.AddListener(() => { PlayButtonSFX(); OpenSettingsSubView(3); });

        // cheats view
        if (cheatReviveAllBtn != null) cheatReviveAllBtn.onClick.AddListener(ExecuteCheatReviveAll);
        if (cheatsBackBtn != null) cheatsBackBtn.onClick.AddListener(() => { PlayButtonSFX(); OpenSettingsSubView(0); });

        // audio view sliders
        if (masterVolumeSlider != null) masterVolumeSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetMasterVolume(val));
        if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetBGMVolume(val));
        if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.AddListener(val => AudioManager.Instance?.SetSFXVolume(val));
        if (audioBackBtn != null) audioBackBtn.onClick.AddListener(() => { PlayButtonSFX(); OpenSettingsSubView(0); });

        // main menu confirm view
        if (confirmMainMenuYesBtn != null) confirmMainMenuYesBtn.onClick.AddListener(ExecuteGoToMainMenu);
        if (confirmMainMenuNoBtn != null) confirmMainMenuNoBtn.onClick.AddListener(() => { PlayButtonSFX(); OpenSettingsSubView(0); });
    }

    private void Start()
    {
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        OpenSettingsSubView(0);
    }

    private void Update()
    {
        CheckHotkeyInputs();
    }

    private void CheckHotkeyInputs()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueRunning) return;
        if (EndGameUIManager.Instance != null && EndGameUIManager.Instance.IsEndGameActive) return;

        bool escapeOrP = false;
        if (Keyboard.current != null)
        {
            escapeOrP = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame;
        }

        if (escapeOrP)
        {
            if (isPaused) ResumeGame();
            else PauseGame(2);
            return;
        }

        bool iOrB = false;
        if (Keyboard.current != null)
        {
            iOrB = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame;
        }

        if (iOrB)
        {
            if (isPaused)
            {
                if (currentActiveTab == 1) ResumeGame();
                else { PlayTabSFX(); SwitchTab(1); }
            }
            else
            {
                PauseGame(1);
            }
        }
    }

    public void PauseGame(int startingTabIndex)
    {
        isPaused = true;

        if (pauseOpenSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(pauseOpenSFX, transform.position, 1.0f);

        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);

        PartyHUD.Instance?.HideHUD();
        OpenSettingsSubView(0); // reset settings to main buttons view
        SwitchTab(startingTabIndex);

        if (PauseCameraDirector.Instance != null)
        {
            PauseCameraDirector.Instance.AnimateToPauseView(true, () =>
            {
                Time.timeScale = 0f;
            });
        }
        else
        {
            Time.timeScale = 0f;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);

        PartyHUD.Instance?.ShowHUD();

        if (PauseCameraDirector.Instance != null)
        {
            PauseCameraDirector.Instance.AnimateToPauseView(false);
        }
    }

    public void SwitchTab(int tabIndex)
    {
        currentActiveTab = tabIndex;

        if (partyPanel != null) partyPanel.SetActive(tabIndex == 0);
        if (inventoryPanel != null) inventoryPanel.SetActive(tabIndex == 1);
        if (settingsPanel != null) settingsPanel.SetActive(tabIndex == 2);

        if (tabIndex == 2)
        {
            OpenSettingsSubView(0);
        }
    }

    // switches between settings sub-menus: 0 = main, 1 = cheats, 2 = audio, 3 = confirm main menu
    public void OpenSettingsSubView(int subViewIndex)
    {
        if (settingsMainView != null) settingsMainView.SetActive(subViewIndex == 0);
        if (cheatsSubView != null) cheatsSubView.SetActive(subViewIndex == 1);
        if (audioSubView != null) audioSubView.SetActive(subViewIndex == 2);
        if (mainMenuConfirmSubView != null) mainMenuConfirmSubView.SetActive(subViewIndex == 3);
    }

    private void ExecuteCheatReviveAll()
    {
        PlayButtonSFX();

        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.ReviveAllDead(1.0f);
        }

        ResumeGame();
    }

    private void ExecuteGoToMainMenu()
    {
        PlayButtonSFX();

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        }
    }

    private void PlayTabSFX()
    {
        if (tabSwitchSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(tabSwitchSFX, transform.position, 0.8f);
    }

    private void PlayButtonSFX()
    {
        if (buttonClickSFX != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(buttonClickSFX, transform.position, 0.8f);
    }

    private void ExecuteCheatKillNearbyEnemies()
    {
        PlayButtonSFX();

        if (PartyManager.Instance != null && PartyManager.Instance.ActivePlayerObj != null)
        {
            Vector2 leaderPos = PartyManager.Instance.ActivePlayerObj.transform.position;

            // find all colliders within the kill radius
            Collider2D[] hits = Physics2D.OverlapCircleAll(leaderPos, cheatKillRadius);

            foreach (var col in hits)
            {
                // ignore friendly party members
                if (col.CompareTag("Player") || col.CompareTag("Ally")) continue;

                // deal 99999 damage to wipe out enemy
                if (col.TryGetComponent<Damageable>(out var enemyHealth))
                {
                    enemyHealth.TakeDamage(99999);
                }
                else if (col.GetComponentInParent<Damageable>() is Damageable parentHealth)
                {
                    parentHealth.TakeDamage(99999);
                }
            }
        }

        // unpause and return to gameplay immediately
        ResumeGame();
    }
}