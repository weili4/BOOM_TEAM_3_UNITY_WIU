using UnityEngine;

public class ComboCounter : MonoBehaviour
{
    [SerializeField] private int maxCombo = 5;
    private int currentCombo = 0;
    private float resetTimer = 2.0f;

    [SerializeField] private IntGameEvent comboIncreasedEvent;
    [SerializeField] private BoolGameEvent comboReadyEvent;

    private void Update()
    {
        if (currentCombo > 0)
        {
            resetTimer -= Time.deltaTime;
            if (resetTimer <= 0)
            {
                currentCombo = 0;
            }
        }
    }

    public void OnEnemyHit()
    {
        currentCombo++;
        resetTimer = 2.0f;

        comboIncreasedEvent?.Raise(currentCombo);

        if (currentCombo >= maxCombo)
        {
            comboReadyEvent?.Raise(true);
            currentCombo = 0;
        }
    }
}