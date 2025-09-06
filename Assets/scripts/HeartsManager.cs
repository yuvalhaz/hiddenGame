using UnityEngine;
using UnityEngine.UI;

public class HeartsManager : MonoBehaviour
{
    [Header("Hearts Logic")]
    [SerializeField] private int hearts = 5;

    [Header("UI Elements")]
    [Tooltip("CanvasGroup של פופאפ/פס 'אין לבבות' שיופיע רק כש-hearts == 0")]
    [SerializeField] private CanvasGroup heartsGroup;

    [Tooltip("טקסט להצגת מספר הלבבות שנותרו (UnityEngine.UI.Text, לא TMP)")]
    [SerializeField] private Text heartsText;

    private void OnEnable()
    {
        // ודא שה-UI משקף את המצב הנוכחי מיד כשמתחילים
        UpdateUI();
    }

    // ===== API (אל תשנה שמות חתימות – קוד אחר תלוי בזה) =====
    public bool HasHearts() => hearts > 0;

    public void SpendHeart(int amount)
    {
        int a = Mathf.Abs(amount);
        hearts = Mathf.Max(0, hearts - a);
        UpdateUI();
    }

    // שמרנו גם את AddHearts וגם את AddHeart לפי המניפסט בפרויקט
    public void AddHearts(int amount) => AddHeart(amount);

    public void AddHeart(int amount = 1)
    {
        int a = Mathf.Abs(amount);
        hearts += a;
        UpdateUI();
    }

    public void LoseHeart(int amount = 1) => SpendHeart(amount);

    // ===== UI Update (הצג קבוצה רק כשאין לבבות) =====
    private void UpdateUI()
    {
        // עדכון טקסט
        if (heartsText != null)
            heartsText.text = hearts.ToString();

        // הצגת/הסתרת CanvasGroup לפי hearts
        if (heartsGroup != null)
        {
            bool noHearts = hearts <= 0;

            heartsGroup.alpha = noHearts ? 1f : 0f;
            heartsGroup.interactable = noHearts;
            heartsGroup.blocksRaycasts = noHearts;
        }
    }
}
