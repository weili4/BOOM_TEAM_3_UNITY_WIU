using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;

public class EndGameUIManager : MonoBehaviour
{
    public static EndGameUIManager Instance { get; private set; }

    [Header("win screen")]
    [SerializeField] private GameObject winPanelRoot;
    [SerializeField] private RectTransform winPanelRect;
    [SerializeField] private Button winMainMenuButton;

    [Header("lose screen")]
    [SerializeField] private GameObject losePanelRoot;
    [SerializeField] private RectTransform losePanelRect;
    [SerializeField] private Button loseReviveButton;
    [SerializeField] private Button loseMainMenuButton;

    [Header("landing flash overlay")]
    [SerializeField] private Image flashOverlayImage;
    [SerializeField] private float flashDuration = 0.35f;

    [Header("slam animation settings")]
    [SerializeField] private float slamDuration = 0.32f;
    [SerializeField] private float topStartOffsetY = 900f; // offscreen top position

    [Header("gameplay ui to hide")]
    [SerializeField] private GameObject abilitiesPanel;
    [SerializeField] private GameObject dialoguePanelRoot;

    [Header("scene names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("audio clips")]
    [SerializeField] private AudioClip winMusic;
    [SerializeField] private AudioClip loseMusic;
    [SerializeField] private AudioClip slamSound;

    private bool isEndGameActive = false;
    public bool IsEndGameActive => isEndGameActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        isEndGameActive = false;
        ForceHidePanels();

        if (winMainMenuButton != null) winMainMenuButton.onClick.AddListener(GoToMainMenu);
        if (loseMainMenuButton != null) loseMainMenuButton.onClick.AddListener(GoToMainMenu);
        if (loseReviveButton != null) loseReviveButton.onClick.AddListener(ReviveParty);
    }


    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        isEndGameActive = false;
        ForceHidePanels();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ForceHidePanels();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isEndGameActive = false;
        ForceHidePanels();
    }
    private void ForceHidePanels()
    {
        if (winPanelRoot != null) winPanelRoot.SetActive(false);
        if (losePanelRoot != null) losePanelRoot.SetActive(false);
        if (flashOverlayImage != null)
        {
            Color c = flashOverlayImage.color;
            c.a = 0f;
            flashOverlayImage.color = c;
            flashOverlayImage.gameObject.SetActive(false);
        }
    }

    public void TriggerWin()
    {
        if (isEndGameActive) return;
        isEndGameActive = true;

        Time.timeScale = 0f;
        HideGameplayUI();

        if (winMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(winMusic, 0.2f);

        if (winPanelRoot != null && winPanelRect != null)
        {
            winPanelRoot.SetActive(true);
            StartCoroutine(SlamPanelRoutine(winPanelRect, Color.white));
        }
    }

    public void TriggerLose()
    {
        if (isEndGameActive) return;
        isEndGameActive = true;

        Time.timeScale = 0f;
        HideGameplayUI();

        if (loseMusic != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayMusic(loseMusic, 0.2f);

        if (losePanelRoot != null && losePanelRect != null)
        {
            losePanelRoot.SetActive(true);
            StartCoroutine(SlamPanelRoutine(losePanelRect, Color.red));
        }
    }

    private void HideGameplayUI()
    {
        PartyHUD.Instance?.HideHUD();
        if (abilitiesPanel != null) abilitiesPanel.SetActive(false);
        if (dialoguePanelRoot != null) dialoguePanelRoot.SetActive(false);
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            PauseMenuManager.Instance.ResumeGame();
        }
    }

    private IEnumerator SlamPanelRoutine(RectTransform panelRect, Color landingFlashColor)
    {
        Vector2 targetPos = Vector2.zero;
        Vector2 startPos = new Vector2(0f, topStartOffsetY);

        panelRect.anchoredPosition = startPos;
        float elapsed = 0f;

        while (elapsed < slamDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slamDuration);

            float ease = t * t * t;
            panelRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, ease);

            yield return null;
        }

        panelRect.anchoredPosition = targetPos;

        if (slamSound != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(slamSound, Vector3.zero, 1.3f);

        StartCoroutine(ScreenFlashRoutine(landingFlashColor));
    }

    private IEnumerator ScreenFlashRoutine(Color flashColor)
    {
        if (flashOverlayImage == null) yield break;

        flashOverlayImage.gameObject.SetActive(true);
        flashOverlayImage.color = flashColor;

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashDuration);

            Color c = flashColor;
            c.a = Mathf.Lerp(0.85f, 0f, t);
            flashOverlayImage.color = c;

            yield return null;
        }

        flashOverlayImage.gameObject.SetActive(false);
    }

    public void ReviveParty()
    {
        isEndGameActive = false;

        // 1. stop all slam routines and force hide lose panel and flash overlay
        StopAllCoroutines();

        if (losePanelRoot != null)
        {
            losePanelRoot.SetActive(false);
        }

        if (flashOverlayImage != null)
        {
            Color c = flashOverlayImage.color;
            c.a = 0f;
            flashOverlayImage.color = c;
            flashOverlayImage.gameObject.SetActive(false);
        }

        // 2. revive all party members and reset hp
        if (PartyManager.Instance != null)
        {
            PartyManager.Instance.ReviveAllDead(1.0f, false);
        }

        // 3. restore gameplay hud
        PartyHUD.Instance?.ShowHUD();
        if (abilitiesPanel != null) abilitiesPanel.SetActive(true);

        // 4. unpause gameplay
        Time.timeScale = 1f;
    }


    public void GoToMainMenu()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}