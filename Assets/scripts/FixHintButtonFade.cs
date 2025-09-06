using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class FixHintButtonFade : MonoBehaviour
{
    public float delaySeconds = 0f; // אם תרצה להמתין עוד קצת

    private IEnumerator Start()
    {
        var btn = GetComponent<Button>();
        btn.interactable = false;          // כיבוי רגעי
        if (delaySeconds <= 0f) yield return null; 
        else yield return new WaitForSecondsRealtime(delaySeconds);
        btn.interactable = true;           // הדלקה חזרה
    }
}
