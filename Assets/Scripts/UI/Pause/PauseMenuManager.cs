using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }

    [Header("root panel")]
    [SerializeField] private GameObject pausePanelRoot;

    [Header("top tab buttons")]
    [SerializeField] private Button partyTabButton;
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button settingsTabButton;

    [Header("tab sub-panels")]
    [SerializeField] private GameObject partyPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingsPanel;

    private bool isPaused = false;
    private bool isTransitioning = false;
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
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
    }

    private void Update()
    {
        if (isTransitioning) return;
        CheckHotkeyInputs();
    }

    private void CheckHotkeyInputs()
    {
        bool escapeOrP = false;
        if (Keyboard.current != null)
        {
            escapeOrP = Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame;
        }

        if (escapeOrP)
        {
            if (isPaused) ResumeGame();
            else PauseGame(2); // open to settings
            return;
        }

        bool iOrB = false;
        if (Keyboard.current != null)
        {
            iOrB = Keyboard.current.iKey.wasPressedThisFrame || Keyboard.current.bKey.wasPressedThisFrame;
        }

        if (iOrB)
        {
            if (isPaused && inventoryPanel != null && inventoryPanel.activeSelf) ResumeGame();
            else PauseGame(1); // open to inventory
        }
    }

    public void PauseGame(int startingTabIndex)
    {
        if (isPaused || isTransitioning) return;
        isPaused = true;
        isTransitioning = true;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(true);
        SwitchTab(startingTabIndex);

        // tilt camera for 10 frames while animator plays then freeze timescale
        if (PauseCameraDirector.Instance != null)
        {
            PauseCameraDirector.Instance.AnimateToPauseView(true, () =>
            {
                Time.timeScale = 0f; // freeze time after 10-frame transition completes
                isTransitioning = false;
            });
        }
        else
        {
            Time.timeScale = 0f;
            isTransitioning = false;
        }
    }

    public void ResumeGame()
    {
        if (!isPaused || isTransitioning) return;
        isPaused = false;
        isTransitioning = true;

        // unpause time instantly
        Time.timeScale = 1f;

        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);

        // return camera to normal 2d view
        if (PauseCameraDirector.Instance != null)
        {
            PauseCameraDirector.Instance.AnimateToPauseView(false, () =>
            {
                isTransitioning = false;
            });
        }
        else
        {
            isTransitioning = false;
        }
    }

    public void SwitchTab(int tabIndex)
    {
        if (partyPanel != null) partyPanel.SetActive(tabIndex == 0);
        if (inventoryPanel != null) inventoryPanel.SetActive(tabIndex == 1);
        if (settingsPanel != null) settingsPanel.SetActive(tabIndex == 2);
    }
}