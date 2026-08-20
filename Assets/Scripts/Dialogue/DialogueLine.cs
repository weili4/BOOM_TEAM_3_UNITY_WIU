using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialogueChoiceOption
{
    public string choiceText = "Option";
    // set to line number to jump ahead, or -1 to just go to the next line
    public int jumpToLineIndex = -1;
}

[System.Serializable]
public class DialogueLine
{
    [Header("speaker info")]
    public string speakerName = "Cool";
    public Sprite speakerPortrait;

    [Header("dialogue text")]
    [TextArea(2, 5)]
    public string sentence = "Enter line of dialogue here...";
    public float typingSpeed = 0.03f; // seconds per letter

    [Header("audio and camera punch")]
    public AudioClip voiceBlipSFX;    // custom voice blip for this line
    public bool enableCameraShake = false;
    public float shakeForce = 0.8f;

    [Header("line event (triggers after this line finishes)")]
    public UnityEvent onLineEndEvent;

    [Header("optional choices (pause line progression)")]
    public bool hasChoices = false;
    public DialogueChoiceOption choiceA;
    public DialogueChoiceOption choiceB;
}