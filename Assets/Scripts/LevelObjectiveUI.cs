using UnityEngine;
using TMPro;

public class LevelObjectiveUI : MonoBehaviour
{
    public static LevelObjectiveUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI objectiveText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetObjectiveText(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }
}