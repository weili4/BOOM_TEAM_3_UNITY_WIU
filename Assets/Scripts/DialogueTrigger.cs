using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Events;

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public int appearOnLine;   // line index where choice appears
    public int branchIndex;    // 0 or 1
}

[System.Serializable]
public class DialogueNameAssignment
{
    [Tooltip("Name to display for these lines.")]
    public string speakerName = "Unknown";

    [Tooltip("Dialogue line indices where this name should appear.")]
    public int[] lineIndices;
}

[System.Serializable]
public class DialogueImageAssignment
{
    [Tooltip("Image to display for these lines.")]
    public Sprite speakerImage;

    [Tooltip("Dialogue line indices where this image should appear.")]
    public int[] lineIndices;
}

[System.Serializable]
public class DialogueEventAssignment
{
    [Tooltip("Event to trigger after this line ends.")]
    public UnityEvent triggerEvent;

    [Tooltip("Dialogue line indices where this event should fire.")]
    public int[] lineIndices;
}

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private string[] mainDialogueLines;
    [SerializeField] private string[] branchDialogue1;
    [SerializeField] private string[] branchDialogue2;

    [Header("Choices (optional, max 2)")]
    [SerializeField] private DialogueChoice[] choices = new DialogueChoice[2];

    [Header("Names (per dialogue set)")]
    [SerializeField] private DialogueNameAssignment[] mainNameAssignments;
    [SerializeField] private DialogueNameAssignment[] branch1NameAssignments;
    [SerializeField] private DialogueNameAssignment[] branch2NameAssignments;

    [Header("Images (per dialogue set)")]
    [SerializeField] private DialogueImageAssignment[] mainImageAssignments;
    [SerializeField] private DialogueImageAssignment[] branch1ImageAssignments;
    [SerializeField] private DialogueImageAssignment[] branch2ImageAssignments;

    [Header("Events (per dialogue set)")]
    [SerializeField] private DialogueEventAssignment[] mainEventAssignments;
    [SerializeField] private DialogueEventAssignment[] branch1EventAssignments;
    [SerializeField] private DialogueEventAssignment[] branch2EventAssignments;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerImageUI;
    [SerializeField] private Image dialoguePanelImage; // background image for transparency
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private GameObject cinematicBackground;

    [Header("Playback Settings")]
    [SerializeField] private float lineDuration = 3f;
    [SerializeField] private bool autoAdvance = true;
    [SerializeField] private bool isCinematic = false;

    private string[] activeDialogue;
    private DialogueNameAssignment[] activeNameAssignments;
    private DialogueImageAssignment[] activeImageAssignments;
    private DialogueEventAssignment[] activeEventAssignments;
    private int currentLine = 0;
    private bool isActive = false;
    private bool choicesActive = false;
    private bool branchActive = false;
    private int resumeIndex = -1;

    [Header("Typewriter Settings")]
    [SerializeField] private float defaultTypingSpeed = 0.05f; // seconds per character
    [SerializeField] private float[] lineTypingSpeeds; // optional per-line overrides


    private void Start()
    {
        if (choiceButton1 != null) choiceButton1.gameObject.SetActive(false);
        if (choiceButton2 != null) choiceButton2.gameObject.SetActive(false);
        if (cinematicBackground != null) cinematicBackground.SetActive(false);
        if (speakerImageUI != null) speakerImageUI.gameObject.SetActive(false);

        SetPanelAlpha(0); // invisible at start
    }


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isActive)
        {
            isActive = true;
            activeDialogue = mainDialogueLines;
            activeNameAssignments = mainNameAssignments;
            activeImageAssignments = mainImageAssignments;
            currentLine = 0;

            dialoguePanel.SetActive(true);

            if (isCinematic)
            {
                Time.timeScale = 0f;
                if (cinematicBackground != null) cinematicBackground.SetActive(true);
                SetPanelAlpha(60); // partially visible
            }
            else
            {
                SetPanelAlpha(0); // invisible in non cinematic
            }

            StartCoroutine(PlayDialogue());
        }
    }


    private void SetPanelAlpha(float alpha)
    {
        if (dialoguePanelImage != null)
        {
            Color c = dialoguePanelImage.color;
            c.a = alpha / 255f; // convert 0–255 to 0–1
            dialoguePanelImage.color = c;
        }
    }

    private string GetSpeakerName(DialogueNameAssignment[] assignments, int lineIndex)
    {
        foreach (var assignment in assignments)
        {
            if (assignment != null && assignment.lineIndices != null)
            {
                foreach (int idx in assignment.lineIndices)
                {
                    if (idx == lineIndex)
                        return string.IsNullOrWhiteSpace(assignment.speakerName) ? "Unknown" : assignment.speakerName;
                }
            }
        }
        return "Unknown";
    }

    private Sprite GetSpeakerImage(DialogueImageAssignment[] assignments, int lineIndex)
    {
        foreach (var assignment in assignments)
        {
            if (assignment != null && assignment.lineIndices != null)
            {
                foreach (int idx in assignment.lineIndices)
                {
                    if (idx == lineIndex)
                        return assignment.speakerImage;
                }
            }
        }
        return null;
    }

    private IEnumerator PlayDialogue()
    {
        for (; currentLine < activeDialogue.Length; currentLine++)
        {
            string nameToShow = GetSpeakerName(activeNameAssignments, currentLine);
            if (speakerNameText != null) speakerNameText.text = nameToShow;

            Sprite img = GetSpeakerImage(activeImageAssignments, currentLine);
            if (speakerImageUI != null)
            {
                if (img != null)
                {
                    speakerImageUI.sprite = img;
                    speakerImageUI.gameObject.SetActive(true);
                }
                else
                {
                    speakerImageUI.gameObject.SetActive(false);
                }
            }

            yield return StartCoroutine(TypeLine(activeDialogue[currentLine], currentLine));

            // Choices still pause progression
            for (int i = 0; i < choices.Length; i++)
            {
                if (choices[i] != null && currentLine == choices[i].appearOnLine && !branchActive)
                {
                    ShowChoice(i, choices[i]);
                }
            }

            yield return new WaitUntil(() => !choicesActive);

            if (autoAdvance)
            {
                yield return new WaitForSecondsRealtime(lineDuration);
            }
            else
            {
                yield return new WaitUntil(() =>
                    UnityEngine.InputSystem.InputSystem.actions["Interact"].WasPressedThisFrame());
            }

            TriggerEventsForLine(activeEventAssignments, currentLine);

        }

        EndDialogue();
    }

    private void ShowChoice(int index, DialogueChoice choice)
    {
        Button btn = (index == 0) ? choiceButton1 : choiceButton2;
        if (btn == null) return;

        btn.gameObject.SetActive(true);
        btn.GetComponentInChildren<TMP_Text>().text = choice.choiceText;
        choicesActive = true;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            resumeIndex = currentLine + 1;
            HideChoices();
            TriggerBranch(choice.branchIndex);
        });
    }

    private void HideChoices()
    {
        if (choiceButton1 != null) choiceButton1.gameObject.SetActive(false);
        if (choiceButton2 != null) choiceButton2.gameObject.SetActive(false);
        choicesActive = false;
    }

    private void TriggerBranch(int branchIndex)
    {
        StopAllCoroutines();
        branchActive = true;

        switch (branchIndex)
        {
            case 0:
                activeDialogue = branchDialogue1;
                activeNameAssignments = branch1NameAssignments;
                activeImageAssignments = branch1ImageAssignments;
                activeEventAssignments = branch1EventAssignments;
                break;
            case 1:
                activeDialogue = branchDialogue2;
                activeNameAssignments = branch2NameAssignments;
                activeImageAssignments = branch2ImageAssignments;
                activeEventAssignments = branch2EventAssignments;
                break;
            default:
                activeDialogue = mainDialogueLines;
                activeNameAssignments = mainNameAssignments;
                activeImageAssignments = mainImageAssignments;
                activeEventAssignments = mainEventAssignments;
                break;
        }

        currentLine = 0;
        StartCoroutine(PlayBranchDialogue());
    }


    private IEnumerator PlayBranchDialogue()
    {
        for (currentLine = 0; currentLine < activeDialogue.Length; currentLine++)
        {
            string nameToShow = GetSpeakerName(activeNameAssignments, currentLine);
            if (speakerNameText != null) speakerNameText.text = nameToShow;

            Sprite img = GetSpeakerImage(activeImageAssignments, currentLine);
            if (speakerImageUI != null)
            {
                if (img != null)
                {
                    speakerImageUI.sprite = img;
                    speakerImageUI.gameObject.SetActive(true);
                }
                else
                {
                    speakerImageUI.gameObject.SetActive(false);
                }
            }

            yield return StartCoroutine(TypeLine(activeDialogue[currentLine], currentLine));

            if (autoAdvance)
            {
                yield return new WaitForSecondsRealtime(lineDuration);
            }
            else
            {
                yield return new WaitUntil(() =>
                    UnityEngine.InputSystem.InputSystem.actions["Interact"].WasPressedThisFrame());
            }
        }

        TriggerEventsForLine(activeEventAssignments, currentLine);

        if ((activeDialogue == branchDialogue1 || activeDialogue == branchDialogue2) &&
            resumeIndex >= 0 && resumeIndex < mainDialogueLines.Length)
        {
            activeDialogue = mainDialogueLines;
            activeNameAssignments = mainNameAssignments;
            activeImageAssignments = mainImageAssignments;
            currentLine = resumeIndex;
            branchActive = false;
            activeEventAssignments = mainEventAssignments;
            yield return StartCoroutine(PlayDialogue());
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false); // set inactive, and invisible
        SetPanelAlpha(0);

        isActive = false;
        branchActive = false;
        resumeIndex = -1;
        HideChoices();

        if (isCinematic)
        {
            Time.timeScale = 1f;
            if (cinematicBackground != null) cinematicBackground.SetActive(false);
        }

        if (speakerImageUI != null) speakerImageUI.gameObject.SetActive(false);
    }

    private void TriggerEventsForLine(DialogueEventAssignment[] assignments, int lineIndex)
    {
        if (assignments == null) return; // safeguard if no assignments array set

        foreach (var assignment in assignments)
        {
            if (assignment != null && assignment.lineIndices != null)
            {
                foreach (int idx in assignment.lineIndices)
                {
                    if (idx == lineIndex && assignment.triggerEvent != null)
                    {
                        assignment.triggerEvent.Invoke();
                    }
                }
            }
        }
    }

    private IEnumerator TypeLine(string lineText, int lineIndex)
    {
        dialogueText.text = "";
        float typingSpeed = defaultTypingSpeed;

        if (lineTypingSpeeds != null && lineIndex < lineTypingSpeeds.Length)
            typingSpeed = lineTypingSpeeds[lineIndex];

        for (int i = 0; i < lineText.Length; i++)
        {
            dialogueText.text += lineText[i];
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }







}
