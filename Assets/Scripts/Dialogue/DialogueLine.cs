using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class DialogueChoiceOption
{
    public string choiceText = "Option";
    public int jumpToLineIndex = -1; // set to target line index or -1 for next line
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
    public float typingSpeed = 0.03f;

    [Header("line event (triggers after this line finishes)")]
    public UnityEvent onLineEndEvent;

    [Header("optional choices")]
    public bool hasChoices = false;
    public DialogueChoiceOption choiceA;
    public DialogueChoiceOption choiceB;
}