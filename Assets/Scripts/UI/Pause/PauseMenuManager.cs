using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("root panel to show/hide")]
    [SerializeField] private GameObject pausePanelRoot; // the Panel GameObject inside Canvas

    [Header("top tab buttons")]
    [SerializeField] private Button partyTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button settingsTabButton;

    [Header("tab sub-panels")]
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingsPanel;

    private int currentActiveTab = 2; // 0 = party, 1 = inventory, 2 = settings
    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (partyTabButton != null) partyTabButton.onClick.AddListener(() => SwitchTab(0));
        if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(() => SwitchTab(1));
        if (settingsTabButton != null) settingsTabButton.onClick.AddListener(() => SwitchTab(2));
    }

    private void Start()
    {
        // ensure game starts unpaused and menu hidden
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Update()
    {
        CheckHotkeyInputs();
    }

    private void CheckHotkeyInputs()
    {
        // block opening pause menu if a cutscene or dialogue is currently running
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueRunning) return;
        if (EndGameUIManager.Instance != null && EndGameUIManager.Instance.IsEndGameActive) return;

        bool escapeOrP = false;

        // 1. check escape or P (settings toggle)
        if (Keyboard.current != null)
        {
            escapeOrP = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame;
        }

        if (escapeOrP)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame(2); // open directly to settings
            }
            return;
        }

        // 2. check I or B (inventory toggle)
        bool iOrB = false;
        if (Keyboard.current != null)
        {
            iOrB = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame;
        }

        if (iOrB)
        {
            if (isPaused)
            {
                // if already on inventory tab pressing I closes the menu
                if (currentActiveTab == 1)
                {
                    ResumeGame();
                }
                else
                {
                    // if paused on another tab switch to inventory
                    SwitchTab(1);
                }
            }
            else
            {
                // open directly to inventory with 3d camera tilt
                PauseGame(1);
            }
        }
    }

    public void PauseGame(int startingTabIndex)
    {
        isPaused = true;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);
        SwitchTab(startingTabIndex);

        // hide party hud with animation
        PartyHUD.Instance?.HideHUD();

        // tilt camera for 10 frames then freeze timescale to 0
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

        // unpause time instantly
        Time.timeScale = 1f;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);

        // show party hud with animation
        PartyHUD.Instance?.ShowHUD();

        // return camera to flat 2d view
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
    }
}