using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Cinemachine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("cinematic dialogue ui (mode 1)")]
    [SerializeField] private GameObject cinematicRoot;            // parent dialogue box container
    [SerializeField] private GameObject cinematicBackground;      // background image for the dialogue box
    [SerializeField] private TMP_Text cinematicNameText;
    [SerializeField] private TMP_Text cinematicSentenceText;
    [SerializeField] private Image cinematicPortraitImage;
    [SerializeField] private RectTransform cinematicPortraitRect;
    [SerializeField] private GameObject cinematicBackdrop;        // dark screen dim overlay

    [Header("subtitle dialogue ui (mode 2)")]
    [SerializeField] private GameObject subtitleRoot;             // floating movie subtitle banner
    [SerializeField] private TMP_Text subtitleNameText;
    [SerializeField] private TMP_Text subtitleSentenceText;
    [SerializeField] private Image subtitlePortraitImage;

    [Header("cinematic choice buttons")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private Button choiceButtonA;
    [SerializeField] private Button choiceButtonB;
    [SerializeField] private TMP_Text choiceTextA;
    [SerializeField] private TMP_Text choiceTextB;

    [Header("gameplay ui to hide during cinematic only")]
    [SerializeField] private GameObject abilitiesPanel;

    [Header("portrait animation settings (cinematic)")]
    [SerializeField] private float portraitSlideDistance = 24f;
    [SerializeField] private float portraitAnimDuration = 0.18f;

    [Header("voice blip audio")]
    [SerializeField] private AudioClip defaultVoiceBlip;
    [SerializeField] private AudioSource audioSource;

    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private int currentLineIndex = 0;
    private bool isDialogueRunning = false;
    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private bool isCinematic = true;
    private System.Action onCompleteCallback;

    private Coroutine typingRoutine;
    private Coroutine portraitAnimRoutine;
    private Sprite lastDisplayedPortrait = null;
    private Vector2 portraitRestingPosition;
    private bool hasSavedPortraitPos = false;

    public bool IsDialogueRunning => isDialogueRunning;
    public bool IsCinematicActive => isDialogueRunning && isCinematic;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (cinematicPortraitRect != null)
        {
            portraitRestingPosition = cinematicPortraitRect.anchoredPosition;
            hasSavedPortraitPos = true;
        }
    }

    private void Start()
    {
        // ensure all dialogue panels start completely hidden
        HideAllDialogueUI();
    }

    private void HideAllDialogueUI()
    {
        if (cinematicRoot != null) cinematicRoot.SetActive(false);
        if (cinematicBackground != null) cinematicBackground.SetActive(false);
        if (cinematicPortraitImage != null) cinematicPortraitImage.gameObject.SetActive(false);
        if (subtitleRoot != null) subtitleRoot.SetActive(false);
        if (choiceContainer != null) choiceContainer.SetActive(false);
        if (cinematicBackdrop != null) cinematicBackdrop.SetActive(false);
    }

    private void Update()
    {
        // in subtitle mode, player cannot skip or advance dialogue manually
        if (!isDialogueRunning || isWaitingForChoice || !isCinematic) return;

        // only check manual skip/advance keys during cinematic cutscene mode
        bool advancePressed = false;
        if (Keyboard.current != null)
        {
            advancePressed = Keyboard.current.eKey.wasPressedThisFrame ||
                             Keyboard.current.fKey.wasPressedThisFrame ||
                             Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        if (!advancePressed && Mouse.current != null)
        {
            advancePressed = Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (advancePressed)
        {
            if (isTyping)
            {
                CompleteSentenceInstantly();
            }
            else
            {
                AdvanceToNextLine();
            }
        }
    }

    public void StartDialogue(List<DialogueLine> lines, bool cinematicMode, System.Action onComplete = null)
    {
        if (lines == null || lines.Count == 0) return;

        isDialogueRunning = true;
        isCinematic = cinematicMode;
        currentLines = lines;
        currentLineIndex = 0;
        onCompleteCallback = onComplete;
        lastDisplayedPortrait = null;

        // 1. cinematic mode: lock player input, hide hud, show full dialogue box and background
        if (isCinematic)
        {
            LockPlayerControls(true);
            PartyHUD.Instance?.HideHUD();
            if (abilitiesPanel != null) abilitiesPanel.SetActive(false);

            if (cinematicRoot != null) cinematicRoot.SetActive(true);
            if (cinematicBackground != null) cinematicBackground.SetActive(true);
            if (subtitleRoot != null) subtitleRoot.SetActive(false);
            if (cinematicBackdrop != null) cinematicBackdrop.SetActive(true);
        }
        // 2. subtitle mode: player can move/fight, hud stays visible, no background dim
        else
        {
            LockPlayerControls(false);

            if (cinematicRoot != null) cinematicRoot.SetActive(false);
            if (cinematicBackground != null) cinematicBackground.SetActive(false);
            if (subtitleRoot != null) subtitleRoot.SetActive(true);
            if (cinematicBackdrop != null) cinematicBackdrop.SetActive(false);
        }

        DisplayLine(currentLineIndex);
    }

    private void DisplayLine(int index)
    {
        if (index < 0 || index >= currentLines.Count)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentLines[index];

        TMP_Text nameTarget = isCinematic ? cinematicNameText : subtitleNameText;
        TMP_Text sentenceTarget = isCinematic ? cinematicSentenceText : subtitleSentenceText;

        if (nameTarget != null)
        {
            nameTarget.text = line.speakerName;
        }

        if (isCinematic)
        {
            UpdateCinematicPortrait(line.speakerPortrait);
        }
        else
        {
            UpdateSubtitlePortrait(line.speakerPortrait);
        }

        if (line.enableCameraShake)
        {
            CinemachineImpulseSource impulse = GetComponent<CinemachineImpulseSource>();
            if (impulse != null) impulse.GenerateImpulse(line.shakeForce);
        }

        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeSentenceRoutine(line, sentenceTarget));
    }

    private void UpdateCinematicPortrait(Sprite newPortrait)
    {
        if (cinematicPortraitImage == null) return;

        if (newPortrait == null)
        {
            cinematicPortraitImage.gameObject.SetActive(false);
            lastDisplayedPortrait = null;
            return;
        }

        cinematicPortraitImage.gameObject.SetActive(true);

        if (newPortrait != lastDisplayedPortrait)
        {
            cinematicPortraitImage.sprite = newPortrait;
            lastDisplayedPortrait = newPortrait;

            if (portraitAnimRoutine != null) StopCoroutine(portraitAnimRoutine);
            portraitAnimRoutine = StartCoroutine(AnimatePortraitEntry());
        }
    }

    private IEnumerator AnimatePortraitEntry()
    {
        if (cinematicPortraitRect == null || cinematicPortraitImage == null) yield break;

        if (!hasSavedPortraitPos)
        {
            portraitRestingPosition = cinematicPortraitRect.anchoredPosition;
            hasSavedPortraitPos = true;
        }

        Vector2 startPos = portraitRestingPosition + new Vector2(-portraitSlideDistance, 0f);
        Vector2 targetPos = portraitRestingPosition;

        cinematicPortraitRect.anchoredPosition = startPos;
        Color c = cinematicPortraitImage.color;
        c.a = 0f;
        cinematicPortraitImage.color = c;

        float elapsed = 0f;

        while (elapsed < portraitAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / portraitAnimDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            cinematicPortraitRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);
            c.a = Mathf.Lerp(0f, 1f, smooth);
            cinematicPortraitImage.color = c;

            yield return null;
        }

        cinematicPortraitRect.anchoredPosition = targetPos;
        c.a = 1f;
        cinematicPortraitImage.color = c;
        portraitAnimRoutine = null;
    }

    private void UpdateSubtitlePortrait(Sprite newPortrait)
    {
        if (subtitlePortraitImage == null) return;

        if (newPortrait != null)
        {
            subtitlePortraitImage.sprite = newPortrait;
            subtitlePortraitImage.gameObject.SetActive(true);
        }
        else
        {
            subtitlePortraitImage.gameObject.SetActive(false);
        }
    }

    private IEnumerator TypeSentenceRoutine(DialogueLine line, TMP_Text targetText)
    {
        isTyping = true;
        targetText.text = "";

        float speed = line.typingSpeed > 0f ? line.typingSpeed : 0.03f;
        int soundCounter = 0;

        for (int i = 0; i < line.sentence.Length; i++)
        {
            targetText.text += line.sentence[i];

            if (line.sentence[i] != ' ' && soundCounter % 2 == 0)
            {
                AudioClip blip = line.voiceBlipSFX != null ? line.voiceBlipSFX : defaultVoiceBlip;
                if (blip != null && audioSource != null)
                {
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(blip, 0.6f);
                }
            }

            soundCounter++;
            yield return new WaitForSeconds(speed);
        }

        isTyping = false;
        typingRoutine = null;

        line.onLineEndEvent?.Invoke();

        if (line.hasChoices && isCinematic)
        {
            ShowChoices(line);
        }
        // subtitle mode waits a reading duration then advances automatically without player input
        else if (!isCinematic)
        {
            float readDelay = Mathf.Max(1.8f, line.sentence.Length * 0.05f);
            yield return new WaitForSeconds(readDelay);
            AdvanceToNextLine();
        }
    }

    private void CompleteSentenceInstantly()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);

        DialogueLine line = currentLines[currentLineIndex];
        TMP_Text targetText = isCinematic ? cinematicSentenceText : subtitleSentenceText;

        if (targetText != null) targetText.text = line.sentence;
        isTyping = false;
        typingRoutine = null;

        line.onLineEndEvent?.Invoke();

        if (line.hasChoices && isCinematic)
        {
            ShowChoices(line);
        }
    }

    private void AdvanceToNextLine()
    {
        currentLineIndex++;
        DisplayLine(currentLineIndex);
    }

    private void ShowChoices(DialogueLine line)
    {
        isWaitingForChoice = true;
        if (choiceContainer != null) choiceContainer.SetActive(true);

        if (choiceButtonA != null && line.choiceA != null)
        {
            choiceButtonA.gameObject.SetActive(true);
            if (choiceTextA != null) choiceTextA.text = line.choiceA.choiceText;
            choiceButtonA.onClick.RemoveAllListeners();
            choiceButtonA.onClick.AddListener(() => OnChoiceSelected(line.choiceA.jumpToLineIndex));
        }

        if (choiceButtonB != null && line.choiceB != null)
        {
            choiceButtonB.gameObject.SetActive(true);
            if (choiceTextB != null) choiceTextB.text = line.choiceB.choiceText;
            choiceButtonB.onClick.RemoveAllListeners();
            choiceButtonB.onClick.AddListener(() => OnChoiceSelected(line.choiceB.jumpToLineIndex));
        }
    }

    private void OnChoiceSelected(int jumpToIndex)
    {
        isWaitingForChoice = false;
        if (choiceContainer != null) choiceContainer.SetActive(false);

        if (jumpToIndex >= 0 && jumpToIndex < currentLines.Count)
        {
            currentLineIndex = jumpToIndex;
        }
        else
        {
            currentLineIndex++;
        }

        DisplayLine(currentLineIndex);
    }

    public void EndDialogue()
    {
        isDialogueRunning = false;
        isTyping = false;
        isWaitingForChoice = false;

        // hide all dialogue ui completely
        HideAllDialogueUI();

        // restore player movement and gameplay hud
        LockPlayerControls(false);
        PartyHUD.Instance?.ShowHUD();
        if (abilitiesPanel != null) abilitiesPanel.SetActive(true);

        onCompleteCallback?.Invoke();
    }

    private void LockPlayerControls(bool locked)
    {
        if (PartyManager.Instance == null || PartyManager.Instance.ActivePlayerObj == null) return;

        if (PartyManager.Instance.ActivePlayerObj.TryGetComponent<PlayerController>(out var controller))
        {
            controller.isInputLocked = locked;

            if (locked)
            {
                controller.ClearForcedVelocity();
            }
        }
    }
}