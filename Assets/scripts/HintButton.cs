using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HintButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("🎯 Hint System")]
    [SerializeField] private VisualHintSystem visualHintSystem;
    [Tooltip("גרור כאן את VisualHintSystem - נדרש לאנימציית הרמז")]

    [Header("Hint Dialog (for Rewarded Ads)")]
    [SerializeField] private HintDialog hintDialog;
    [Tooltip("Dialog that shows rewarded ad option. Leave empty for direct hints.")]

    [Header("🎓 Tutorial Mode")]
    [SerializeField] private bool isTutorialLevel = false;
    [Tooltip("אם מסומן - רמז יופעל ישירות ללא דיאלוג")]

    [Header("Cooldown Visual")]
    [SerializeField] private Image hintIcon;
    [Tooltip("אייקון הרמז - מוחלף לספרייט cooldown בזמן קולדאון")]
    [SerializeField] private Sprite cooldownSprite;
    [Tooltip("ספרייט שמוצג בזמן cooldown")]

    [Header("Optional")]
    public UnityEvent onPressed;

    private Sprite normalSprite;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);

        // Auto-find hint icon if not assigned
        if (hintIcon == null)
        {
            hintIcon = GetComponent<Image>();
        }
        if (hintIcon != null)
        {
            normalSprite = hintIcon.sprite;
        }

        // Auto-find components if not assigned
        if (visualHintSystem == null)
        {
            visualHintSystem = FindObjectOfType<VisualHintSystem>();
        }

        if (hintDialog == null && !isTutorialLevel)
        {
            hintDialog = FindObjectOfType<HintDialog>();
        }

        // Subscribe to hint granted event
        if (hintDialog != null)
        {
            hintDialog.onHintGranted.AddListener(OnHintGranted);
        }
    }

    private void Update()
    {
        if (hintIcon == null || visualHintSystem == null || cooldownSprite == null) return;

        bool onCooldown = visualHintSystem.IsOnCooldown();
        Sprite targetSprite = onCooldown ? cooldownSprite : normalSprite;

        if (hintIcon.sprite != targetSprite)
        {
            hintIcon.sprite = targetSprite;
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);

        // Unsubscribe from hint granted event
        if (hintDialog != null)
        {
            hintDialog.onHintGranted.RemoveListener(OnHintGranted);
        }
    }

    private void OnHintGranted()
    {
        // Called after watching the ad - trigger the hint animation and consume one hint
        Debug.Log("💰 [HintButton] Hint granted after watching ad - triggering hint!");
        if (TriggerHintAnimation())
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.UseHint();
            }
        }
    }

    private void OnClick()
    {
        // ✅ Notify TutorialSlideManager that hint was clicked
        if (TutorialSlideManager.Instance != null)
        {
            TutorialSlideManager.Instance.OnHintButtonClicked();
        }

        // Tutorial mode - trigger hint directly without dialog
        if (isTutorialLevel)
        {
            Debug.Log("🎓 [HintButton] Tutorial mode - triggering hint directly!");
            TriggerHintAnimation();
        }
        // Check if player has purchased hints or unlimited hints
        else if (IAPManager.Instance != null && IAPManager.Instance.CanUseHint())
        {
            Debug.Log("💎 [HintButton] Player has hints - trying to use purchased hint!");
            // ✅ FIX: הפעל רמז קודם, רק אם הצליח - הורד מהקאונטר
            if (TriggerHintAnimation())
            {
                IAPManager.Instance.UseHint();
            }
            else
            {
                Debug.LogWarning("⏳ [HintButton] Hint not triggered (cooldown/active) - not consuming hint");
            }
        }
        // Normal level - open dialog for rewarded ad
        else if (hintDialog != null)
        {
            Debug.Log("💬 [HintButton] No hints available - opening dialog for rewarded ad");
            hintDialog.Open();
        }
        // Fallback - no dialog available, trigger directly
        else
        {
            Debug.LogWarning("⚠️ [HintButton] No HintDialog found - triggering hint directly");
            TriggerHintAnimation();
        }

        onPressed?.Invoke();
    }

    private bool TriggerHintAnimation()
    {
        if (visualHintSystem != null)
        {
            return visualHintSystem.TriggerHint();
        }
        else
        {
            Debug.LogError("❌ [HintButton] VisualHintSystem not assigned!");
            return false;
        }
    }
}
