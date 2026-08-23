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

    [Header("cinematic dialogue ui (bottom-to-up slide & fade)")]
    [SerializeField] private GameObject cinematicRoot;
    [SerializeField] private CanvasGroup cinematicCanvasGroup;
    [SerializeField] private RectTransform cinematicRootRect;
    [SerializeField] private GameObject cinematicBackground;
    [SerializeField] private TMP_Text cinematicNameText;
    [SerializeField] private TMP_Text cinematicSentenceText;
    [SerializeField] private Image cinematicPortraitImage;
    [SerializeField] private RectTransform cinematicPortraitRect;
    [SerializeField] private GameObject cinematicBackdrop;
    [SerializeField] private float cinematicSlideOffsetY = 80f; // slides up from bottom by pixels
    [SerializeField] private float cinematicTransitionDuration = 0.22f;

    [Header("subtitle dialogue ui (right-to-left slide & fade)")]
    [SerializeField] private GameObject subtitleRoot;
    [SerializeField] private CanvasGroup subtitleCanvasGroup;
    [SerializeField] private RectTransform subtitleRootRect;
    [SerializeField] private TMP_Text subtitleNameText;
    [SerializeField] private TMP_Text subtitleSentenceText;
    [SerializeField] private Image subtitlePortraitImage;
    [SerializeField] private float subtitleSlideOffsetX = 120f; // slides in from right
    [SerializeField] private float subtitleLineTransitionDuration = 0.18f;

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

    [SerializeField] private GameObject cinematicContinuePrompt; // the small arrow icon in the corner

    private List<DialogueLine> currentLines = new List<DialogueLine>();
    private int currentLineIndex = 0;
    private bool isDialogueRunning = false;
    private bool isTyping = false;
    private bool isWaitingForChoice = false;
    private bool isCinematic = true;
    private System.Action onCompleteCallback;

    private Coroutine typingRoutine;
    private Coroutine portraitAnimRoutine;
    private Coroutine rootTransitionRoutine;
    private Sprite lastDisplayedPortrait = null;

    private Vector2 cinematicRestingPos;
    private Vector2 subtitleRestingPos;
    private Vector2 portraitRestingPos;
    private bool hasSavedPositions = false;

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

        SaveRestingPositions();
    }

    private void SaveRestingPositions()
    {
        if (hasSavedPositions) return;

        if (cinematicRootRect != null) cinematicRestingPos = cinematicRootRect.anchoredPosition;
        if (subtitleRootRect != null) subtitleRestingPos = subtitleRootRect.anchoredPosition;
        if (cinematicPortraitRect != null) portraitRestingPos = cinematicPortraitRect.anchoredPosition;

        hasSavedPositions = true;
    }

    private void Start()
    {
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
        if (cinematicContinuePrompt != null) cinematicContinuePrompt.SetActive(false);
    }

    private void Update()
    {
        if (!isDialogueRunning || isWaitingForChoice || !isCinematic) return;

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

        SaveRestingPositions();

        isDialogueRunning = true;
        isCinematic = cinematicMode;
        currentLines = lines;
        currentLineIndex = 0;
        onCompleteCallback = onComplete;
        lastDisplayedPortrait = null;

        // 1. cinematic mode: bottom-to-up slide and fade in
        if (isCinematic)
        {
            LockPlayerControls(true);
            PartyHUD.Instance?.HideHUD();
            if (abilitiesPanel != null) abilitiesPanel.SetActive(false);

            if (cinematicRoot != null) cinematicRoot.SetActive(true);
            if (cinematicBackground != null) cinematicBackground.SetActive(true);
            if (subtitleRoot != null) subtitleRoot.SetActive(false);
            if (cinematicBackdrop != null) cinematicBackdrop.SetActive(true);

            StartCoroutine(AnimateCinematicEnter());
        }
        // 2. subtitle mode: right-to-left slide
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

        // hide prompt while new line is starting
        if (cinematicContinuePrompt != null)
        {
            cinematicContinuePrompt.SetActive(false);
        }

        DialogueLine line = currentLines[index];

        if (isCinematic)
        {
            if (cinematicNameText != null) cinematicNameText.text = line.speakerName;
            UpdateCinematicPortrait(line.speakerPortrait);

            if (line.enableCameraShake)
            {
                CinemachineImpulseSource impulse = GetComponent<CinemachineImpulseSource>();
                if (impulse != null) impulse.GenerateImpulse(line.shakeForce);
            }

            if (typingRoutine != null) StopCoroutine(typingRoutine);
            typingRoutine = StartCoroutine(TypeSentenceRoutine(line, cinematicSentenceText));
        }
        else
        {
            if (rootTransitionRoutine != null) StopCoroutine(rootTransitionRoutine);
            rootTransitionRoutine = StartCoroutine(SubtitleLineTransitionRoutine(line));
        }
    }

    private IEnumerator AnimateCinematicEnter()
    {
        if (cinematicRootRect == null) yield break;

        Vector2 startPos = cinematicRestingPos + new Vector2(0f, -cinematicSlideOffsetY);
        Vector2 targetPos = cinematicRestingPos;

        cinematicRootRect.anchoredPosition = startPos;
        if (cinematicCanvasGroup != null) cinematicCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < cinematicTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cinematicTransitionDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            cinematicRootRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);
            if (cinematicCanvasGroup != null) cinematicCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smooth);

            yield return null;
        }

        cinematicRootRect.anchoredPosition = targetPos;
        if (cinematicCanvasGroup != null) cinematicCanvasGroup.alpha = 1f;
    }

    private IEnumerator AnimateCinematicExit(System.Action onFinished)
    {
        if (cinematicRootRect == null)
        {
            onFinished?.Invoke();
            yield break;
        }

        Vector2 startPos = cinematicRestingPos;
        Vector2 targetPos = cinematicRestingPos + new Vector2(0f, -cinematicSlideOffsetY);

        float elapsed = 0f;

        while (elapsed < cinematicTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / cinematicTransitionDuration);
            float smooth = t * t;

            cinematicRootRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);
            if (cinematicCanvasGroup != null) cinematicCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smooth);

            yield return null;
        }

        cinematicRootRect.anchoredPosition = cinematicRestingPos;
        if (cinematicCanvasGroup != null) cinematicCanvasGroup.alpha = 0f;

        onFinished?.Invoke();
    }

    private IEnumerator SubtitleLineTransitionRoutine(DialogueLine line)
    {
        // 1. quick fade out previous line if already showing
        if (subtitleCanvasGroup != null && subtitleCanvasGroup.alpha > 0.05f)
        {
            float fadeOutElapsed = 0f;
            while (fadeOutElapsed < 0.08f)
            {
                fadeOutElapsed += Time.deltaTime;
                subtitleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeOutElapsed / 0.08f);
                yield return null;
            }
        }

        // 2. update subtitle text and portrait while hidden
        if (subtitleNameText != null) subtitleNameText.text = line.speakerName;
        UpdateSubtitlePortrait(line.speakerPortrait);

        // 3. slide in from right to left while fading in
        Vector2 startPos = subtitleRestingPos + new Vector2(subtitleSlideOffsetX, 0f);
        Vector2 targetPos = subtitleRestingPos;

        if (subtitleRootRect != null) subtitleRootRect.anchoredPosition = startPos;
        if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < subtitleLineTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / subtitleLineTransitionDuration);
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            if (subtitleRootRect != null) subtitleRootRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, smooth);
            if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = Mathf.Lerp(0f, 1f, smooth);

            yield return null;
        }

        if (subtitleRootRect != null) subtitleRootRect.anchoredPosition = targetPos;
        if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 1f;

        // 4. start typewriter for subtitle
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        typingRoutine = StartCoroutine(TypeSentenceRoutine(line, subtitleSentenceText));
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

        Vector2 startPos = portraitRestingPos + new Vector2(-portraitSlideDistance, 0f);
        Vector2 targetPos = portraitRestingPos;

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
        if (cinematicContinuePrompt != null) cinematicContinuePrompt.SetActive(false);

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
        // show continue prompt icon when line is fully typed in cinematic mode
        else if (isCinematic)
        {
            if (cinematicContinuePrompt != null)
            {
                cinematicContinuePrompt.SetActive(true);
            }
        }
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
        else if (isCinematic)
        {
            if (cinematicContinuePrompt != null)
            {
                cinematicContinuePrompt.SetActive(true);
            }
        }
    }

    private void AdvanceToNextLine()
    {
        if (cinematicContinuePrompt != null)
        {
            cinematicContinuePrompt.SetActive(false);
        }

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

        if (isCinematic)
        {
            // slide down and fade out before disabling
            StartCoroutine(AnimateCinematicExit(() =>
            {
                HideAllDialogueUI();
                LockPlayerControls(false);
                PartyHUD.Instance?.ShowHUD();
                if (abilitiesPanel != null) abilitiesPanel.SetActive(true);
                onCompleteCallback?.Invoke();
            }));
        }
        else
        {
            HideAllDialogueUI();
            LockPlayerControls(false);
            PartyHUD.Instance?.ShowHUD();
            if (abilitiesPanel != null) abilitiesPanel.SetActive(true);
            onCompleteCallback?.Invoke();
        }
    }

    private void LockPlayerControls(bool locked)
    {
        if (PartyManager.Instance == null || PartyManager.Instance.ActivePlayerObj == null) return;

        if (PartyManager.Instance.ActivePlayerObj.TryGetComponent<PlayerController>(out var controller))
        {
            controller.isInputLocked = locked;
            if (locked) controller.ClearForcedVelocity();
        }
    }
}