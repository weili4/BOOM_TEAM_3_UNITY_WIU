using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("main root panel")]
    [SerializeField] private GameObject pausePanelRoot; // Panel

    [Header("top tab buttons")]
    [SerializeField] private Button partyTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button settingsTabButton;

    [Header("tab sub-panels")]
    [SerializeField] private GameObject partyPanel;     // Party Panel
    [SerializeField] private GameObject inventoryPanel; // Inventory Panel
    [SerializeField] private GameObject settingsPanel;  // Settings Panel

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // hook tab buttons
        if (partyTabButton != null) partyTabButton.onClick.AddListener(() => SwitchTab(0));
        if (inventoryTabButton != null) inventoryTabButton.onClick.AddListener(() => SwitchTab(1));
        if (settingsTabButton != null) settingsTabButton.onClick.AddListener(() => SwitchTab(2));
    }

    private void Start()
    {
        // hide menu on start
        ResumeGame();
    }

    private void Update()
    {
        CheckHotkeyInputs();
    }

    private void CheckHotkeyInputs()
    {
        // 1. check escape or P (settings tab)
        bool escapeOrP = false;
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
                PauseGame(2); // open directly to settings tab
            }
            return;
        }

        // 2. check I or B (inventory tab)
        bool iOrB = false;
        if (Keyboard.current != null)
        {
            iOrB = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame;
        }

        if (iOrB)
        {
            if (isPaused && inventoryPanel.activeSelf)
            {
                ResumeGame();
            }
            else
            {
                PauseGame(1); // open directly to inventory tab
            }
        }
    }

    public void PauseGame(int startingTabIndex)
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);
        SwitchTab(startingTabIndex);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
    }

    public void SwitchTab(int tabIndex)
    {
        // 0 = party, 1 = inventory, 2 = settings
        if (partyPanel != null) partyPanel.SetActive(tabIndex == 0);
        if (inventoryPanel != null) inventoryPanel.SetActive(tabIndex == 1);
        if (settingsPanel != null) settingsPanel.SetActive(tabIndex == 2);
    }
}