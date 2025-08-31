using UnityEngine;
using UnityEngine.UI;

public class HeartsManager : MonoBehaviour
{
    public static HeartsManager Instance { get; private set; }

    [Header("Hearts")]
    public int maxHearts = 5;
    public int currentHearts = 5;

    [Header("Optional UI")]
    public Text heartsText; // אופציונלי להצגת הכמות

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        currentHearts = Mathf.Clamp(currentHearts, 0, maxHearts);
        RefreshUI();
    }

    public bool HasHearts() => currentHearts > 0;

    public bool LoseHeart(int amount = 1)
    {
        if (currentHearts <= 0) return false;
        currentHearts = Mathf.Max(0, currentHearts - Mathf.Max(1, amount));
        RefreshUI();
        return true;
    }

    public void AddHeart(int amount = 1)
    {
        currentHearts = Mathf.Min(maxHearts, currentHearts + Mathf.Max(1, amount));
        RefreshUI();
    }

    void RefreshUI()
    {
        if (heartsText != null) heartsText.text = currentHearts.ToString();
    }
}
