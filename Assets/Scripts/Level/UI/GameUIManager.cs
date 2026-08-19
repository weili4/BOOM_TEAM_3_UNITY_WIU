using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameUIManager : MonoBehaviour
{
    // GAME UI MANAGER WITH PAUSE ON DEATH AND WIN SCREEN

    public static GameUIManager Instance { get; private set; }

    [Header("Ui panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject cheatsPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject inventoryPanel;

    [Header("GAME OVER DELAYED BUTTONS CONTAINER")]
    [SerializeField] private GameObject gameOverButtonsContainer; // to disable later (cause i keep accidentally pressing the buttons)

    [Header("TARGET PLAYER AND TELEPORT POINT")]
    [SerializeField] private Damageable playerDamageable;
    [SerializeField] private Transform teleportEndPoint;

    [Header("SCENE NAMES")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;
    private bool isGameOver = false;

    /*===============================================================================================================*/
    private bool isInventoryOpen= false;
    /*===============================================================================================================*/

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // hide all panels at start
        if (pausePanel != null) pausePanel.SetActive(false);
        if (cheatsPanel != null) cheatsPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        if (playerDamageable == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerDamageable = player.GetComponent<Damageable>();
        }

        if (playerDamageable != null)
        {
            playerDamageable.onHealthChanged.AddListener(OnPlayerHealthChanged);
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        // check pause input via new input system
        bool pausePressed = false;
        var pauseAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Pause") : null; // i have a fallback for the input system, in case my new one breaks 

        if (pauseAction != null)
        {
            pausePressed = pauseAction.WasPressedThisFrame();
        }
        else if (Keyboard.current != null)
        {
            pausePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        /*===================================================================================================================*/
        bool InventoryPressed = false;
        var InventoryAction = InputSystem.actions != null ? InputSystem.actions.FindAction("Inventory") : null;

        if (InventoryAction != null)
        {
            InventoryPressed = InventoryAction.WasPressedThisFrame();
        }
        else if (Keyboard.current != null)
        {
            InventoryPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
        /*===================================================================================================================*/

        if (pausePressed)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /*===============================================================================================================*/
        if (InventoryPressed)
        {
            if(isInventoryOpen)
            {
                ResumeGame();
            }
            else
            {
                InventoryMenu();
            }
        }
        /*===============================================================================================================*/
    }

    /*===================================================================================================================*/
    public void InventoryMenu()
    {
        isInventoryOpen = true;
        Time.timeScale = 0f;

        if (inventoryPanel != null) inventoryPanel.SetActive(true);
    }
    /*===================================================================================================================*/

    /*===================================================================================================================*/
    public bool GetInventoryOpen()
    {
        return isInventoryOpen;
    }
    /*===================================================================================================================*/

    // PAUSE MENU

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (cheatsPanel != null) cheatsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        isPaused = false;
        isInventoryOpen = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (cheatsPanel != null) cheatsPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    // CHEATS MENU
    public void OpenCheatsMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (cheatsPanel != null) cheatsPanel.SetActive(true);
    }

    public void CloseCheatsMenu()
    {
        if (cheatsPanel != null) cheatsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void CheatMaxHealth()
    {
        if (playerDamageable != null)
        {
            playerDamageable.Heal(playerDamageable.MaxHealth);
        }
        ResumeGame();
    }

    public void CheatTeleportToEnd()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && teleportEndPoint != null)
        {
            player.transform.position = teleportEndPoint.position;

            if (player.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        ResumeGame();
    }

    // GAME OVER LOGIC

    private void OnPlayerHealthChanged(int currentHealth, int maxHealth)
    {
        if (currentHealth <= 0 && !isGameOver)
        {
            TriggerGameOver();
        }
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // PAUSE GAME TIME IMMEDIATELY ON DEATH
        Time.timeScale = 0f;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        if (gameOverButtonsContainer != null) gameOverButtonsContainer.SetActive(false); // hide buttons for 2 seconds

        StartCoroutine(ShowGameOverButtonsRoutine());
    }

    private IEnumerator ShowGameOverButtonsRoutine()
    {
        // wait 1 second in real time while game is paused
        yield return new WaitForSecondsRealtime(1.0f);

        if (gameOverButtonsContainer != null)
        {
            gameOverButtonsContainer.SetActive(true);
        }
    }

    public void RevivePlayerDebug()
    {
        isGameOver = false;

        if (playerDamageable != null)
        {
            playerDamageable.Heal(playerDamageable.MaxHealth);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    // WIN SCREEN LOGIC

    public void TriggerWinScreen()
    {
        Time.timeScale = 0f; // pause game time

        if (winPanel != null) winPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (cheatsPanel != null) cheatsPanel.SetActive(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}