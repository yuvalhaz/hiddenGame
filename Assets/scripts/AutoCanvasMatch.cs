using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class AutoCanvasMatch : MonoBehaviour
{
    [Header("Match values")]
    [Range(0f, 1f)] public float phoneMatch = 0.6f;
    [Range(0f, 1f)] public float tabletMatch = 1.0f;

    [Header("Tablet detection")]
    [Tooltip("If aspect ratio is closer to 4:3 (iPad), use tabletMatch")]
    public float tabletAspectThreshold = 1.45f; 
    // 4:3 = 1.333, 16:9 = 1.777. Anything under ~1.45 behaves like tablet-ish.

    CanvasScaler scaler;
    int lastW, lastH;

    void Awake()
    {
        scaler = GetComponent<CanvasScaler>();
        Apply();
    }

    void Update()
    {
        // Re-apply if resolution changes (editor, rotation, etc.)
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }

    void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;

        float aspect = (float)Mathf.Max(Screen.width, Screen.height) / Mathf.Min(Screen.width, Screen.height);

        // tablet-ish (iPad 4:3, etc.) => match height
        bool isTabletLike = aspect < tabletAspectThreshold;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = isTabletLike ? tabletMatch : phoneMatch;
    }
}