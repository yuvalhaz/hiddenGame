using UnityEngine;

public class ResetIAPDebug : MonoBehaviour
{
    private const string ADS_REMOVED_KEY = "AdsRemoved";
    private const string HINTS_COUNT_KEY = "HintsCount";
    private const string UNLIMITED_HINTS_KEY = "UnlimitedHints";

    void Start()
    {
        // איפוס כל רכישות IAP (לבדיקות בלבד)
        PlayerPrefs.SetInt(ADS_REMOVED_KEY, 0);
        PlayerPrefs.SetInt(HINTS_COUNT_KEY, 0);
        PlayerPrefs.SetInt(UNLIMITED_HINTS_KEY, 0);
        PlayerPrefs.Save();

        Debug.Log("🧨 IAP RESET DONE");

        // ✅ עדכון UI בצורה חוקית
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.NotifyHintsChanged();
        }
    }
}
