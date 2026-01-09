using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SmlAnimManager : MonoBehaviour
{
    public static SmlAnimManager Instance { get; private set; }

    [System.Serializable]
    public class SpotButtonLink
    {
        public DropSpot spot;
        public Button button;
    }

    [Header("Links (drag Spot + Button pairs here)")]
    [SerializeField] private List<SpotButtonLink> links = new List<SpotButtonLink>();

    [Header("Auto refresh")]
    [Tooltip("If true: manager will check spots every frame. If false: call RefreshAll() when you want.")]
    [SerializeField] private bool autoRefreshEachFrame = false;

    [Header("Animation")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float wiggleAngle = 10f;

    [Header("Sound (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] clickClips;
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    [Header("Level Complete")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private AudioClip levelCompleteSound;
    [SerializeField] private int confettiCount = 100;
    [SerializeField] private float confettiDuration = 1.5f;

    private readonly HashSet<Button> wired = new HashSet<Button>();
    private readonly Dictionary<Button, Coroutine> running = new Dictionary<Button, Coroutine>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("[SmlAnimManager] Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize all buttons to prevent Unity from making them transparent
        InitializeButtons();
        RefreshAll();
    }

    private void InitializeButtons()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link == null || link.button == null)
                continue;

            var btn = link.button;

            // CRITICAL: Set transition to None to prevent alpha changes
            btn.transition = Selectable.Transition.None;

            // Force full opacity colors
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            btn.colors = colors;

            // CRITICAL: Add CanvasGroup and disable raycasts by default!
            var canvasGroup = btn.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = btn.gameObject.AddComponent<CanvasGroup>();
            }
            // Start with raycasts DISABLED to allow drops
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = true;
            canvasGroup.alpha = 1f;

            Debug.Log($"[SmlAnimManager] Initialized button: {btn.name} with CanvasGroup (blocksRaycasts=false)");
        }
    }

    private void Update()
    {
        if (autoRefreshEachFrame)
            RefreshAll();
    }

    public void RefreshAll()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link == null || link.spot == null || link.button == null)
                continue;

            bool on = link.spot.IsSettled;

            // Only wire the button if the spot is settled
            if (on)
                Wire(link.button);

            ApplyState(link.button, on);
        }
    }

    public void RefreshSpot(DropSpot spot)
    {
        if (spot == null) return;

        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link == null || link.spot != spot || link.button == null)
                continue;

            bool on = spot.IsSettled;

            // Only wire the button if the spot is settled
            if (on)
                Wire(link.button);

            ApplyState(link.button, on);
            return;
        }
    }

    private void ApplyState(Button btn, bool enabled)
    {
        // SIMPLEST SOLUTION: Just control btn.enabled
        // Make sure the button doesn't have any graphics that block raycasts

        btn.enabled = enabled;

        // CRITICAL: Also disable ALL graphics on the same GameObject to prevent blocking
        var graphics = btn.GetComponents<Graphic>();
        foreach (var graphic in graphics)
        {
            if (graphic != null)
            {
                graphic.raycastTarget = enabled;
            }
        }

        Debug.Log($"[SmlAnimManager] ApplyState: {btn.name} -> btn.enabled={enabled}, graphics raycast={enabled}");
    }

    private void Wire(Button btn)
    {
        if (btn == null) return;
        if (wired.Contains(btn)) return;

        // CRITICAL: Disable button transition to prevent Unity from changing alpha/colors
        btn.transition = Selectable.Transition.None;

        // Make sure the button doesn't have color multiplier
        var colors = btn.colors;
        colors.disabledColor = Color.white; // Keep full opacity when disabled
        colors.fadeDuration = 0f;
        btn.colors = colors;

        Button local = btn;
        local.onClick.AddListener(() => OnClicked(local));

        wired.Add(btn);

        Debug.Log($"[SmlAnimManager] Wired button: {btn.name}");
    }

    private void OnClicked(Button btn)
    {
        if (btn == null) return;

        PlayClick();

        RectTransform rt = btn.GetComponent<RectTransform>();
        if (rt == null) return;

        if (running.TryGetValue(btn, out var c) && c != null)
            StopCoroutine(c);

        running[btn] = StartCoroutine(WigglePulse(btn, rt));
    }

    private void PlayClick()
    {
        if (audioSource == null) return;
        if (clickClips == null || clickClips.Length == 0) return;

        var clip = clickClips[Random.Range(0, clickClips.Length)];
        if (clip == null) return;

        audioSource.PlayOneShot(clip, clickVolume);
    }

    private IEnumerator WigglePulse(Button key, RectTransform rt)
    {
        Vector3 startScale = rt.localScale;
        Quaternion startRot = rt.localRotation;

        float time = 0f;
        while (time < duration)
        {
            float u = time / duration;

            float pulseT = (u <= 0.5f) ? (u / 0.5f) : ((1f - u) / 0.5f);
            float s = Mathf.Lerp(1f, pulseScale, pulseT);
            rt.localScale = startScale * s;

            float angle = ComputeWiggle(u, wiggleAngle);
            rt.localRotation = startRot * Quaternion.Euler(0f, 0f, angle);

            time += Time.unscaledDeltaTime;
            yield return null;
        }

        rt.localScale = startScale;
        rt.localRotation = startRot;

        running.Remove(key);
    }

    private float ComputeWiggle(float u, float A)
    {
        float seg = u * 5f;
        int i = Mathf.FloorToInt(seg);
        float t = seg - i;

        float k0, k1;
        switch (i)
        {
            default:
            case 0: k0 = 0f;  k1 = -A; break;
            case 1: k0 = -A;  k1 = +A; break;
            case 2: k0 = +A;  k1 = -A; break;
            case 3: k0 = -A;  k1 = +A; break;
            case 4: k0 = +A;  k1 = 0f;  break;
        }
        return Mathf.Lerp(k0, k1, t);
    }

    public void OnLevelComplete(int levelIndex)
    {
        Debug.Log($"[SmlAnimManager] Level {levelIndex} complete! Triggering confetti.");

        if (levelCompleteSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(levelCompleteSound);
        }

        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                UIConfetti.Burst(canvas, canvasRect, confettiCount, confettiDuration);
            }
        }
    }

    public void TriggerLevelComplete()
    {
        OnLevelComplete(0);
    }
}
