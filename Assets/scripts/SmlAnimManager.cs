using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

    [Header("Cooldown")]
    [SerializeField] private float clickCooldown = 10f;
    [Tooltip("Seconds between clicks on the same button")]

    [Header("Breathe Hint")]
    [SerializeField] private float breatheMinInterval = 15f;
    [SerializeField] private float breatheMaxInterval = 20f;
    [SerializeField] private float breatheScale = 1.08f;
    [SerializeField] private float breatheDuration = 0.6f;

    private readonly HashSet<Button> wired = new HashSet<Button>();
    private readonly Dictionary<Button, Coroutine> running = new Dictionary<Button, Coroutine>();
    private readonly Dictionary<Button, Coroutine> breatheRoutines = new Dictionary<Button, Coroutine>();
    private readonly Dictionary<Button, float> lastClickTime = new Dictionary<Button, float>();

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
            return;
        }

        // Disable ALL buttons IMMEDIATELY in Awake
        InitializeButtons();
    }

    private void Start()
    {
        // Refresh based on settled state
        RefreshAll();
    }

    private void InitializeButtons()
    {
        // DON'T do anything here!
        // Let buttons exist naturally, we'll only add onClick when settled
        Debug.Log("[SmlAnimManager] InitializeButtons - doing nothing, waiting for settled spots");
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
        if (!enabled)
        {
            // When disabled, do NOTHING - let DisableAllButtonsForDrag handle it during drag
            return;
        }

        // When enabled (spot is settled), ensure button is clickable
        // Enable CanvasGroup blocksRaycasts - add CanvasGroup if it doesn't exist
        CanvasGroup cg = btn.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = btn.gameObject.AddComponent<CanvasGroup>();
            Debug.Log($"[SmlAnimManager] Added CanvasGroup to {btn.name}");
        }
        cg.blocksRaycasts = true;

        // Enable raycastTarget on button image
        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.raycastTarget = true;
        }

        // Also ensure graphics children have raycastTarget
        var graphics = btn.GetComponentsInChildren<Graphic>(true);
        foreach (var graphic in graphics)
        {
            graphic.raycastTarget = true;
        }

        Debug.Log($"[SmlAnimManager] {btn.name} -> enabled, blocksRaycasts=true, raycastTarget=true");
    }

    private void Wire(Button btn)
    {
        if (btn == null) return;
        if (wired.Contains(btn)) return;

        // Disable navigation to prevent color bleed between buttons
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        // Simply add the onClick listener, nothing else!
        Button local = btn;
        local.onClick.AddListener(() => OnClicked(local));

        wired.Add(btn);

        // Start breathe hint loop
        if (!breatheRoutines.ContainsKey(btn))
        {
            breatheRoutines[btn] = StartCoroutine(BreatheHintLoop(btn));
        }

        Debug.Log($"[SmlAnimManager] Wired button: {btn.name} - onClick listener added");
    }

    private void OnClicked(Button btn)
    {
        if (btn == null) return;

        // Check cooldown
        float currentTime = Time.time;
        if (lastClickTime.TryGetValue(btn, out float lastTime))
        {
            float timeSinceLastClick = currentTime - lastTime;
            if (timeSinceLastClick < clickCooldown)
            {
                float remaining = clickCooldown - timeSinceLastClick;
                Debug.Log($"[SmlAnimManager] {btn.name} on cooldown - {remaining:F1}s remaining");
                return;
            }
        }

        // Update last click time
        lastClickTime[btn] = currentTime;

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

    private IEnumerator BreatheHintLoop(Button btn)
    {
        // Random initial delay so buttons don't all breathe together
        yield return new WaitForSeconds(Random.Range(3f, breatheMaxInterval));

        while (btn != null && btn.gameObject != null)
        {
            // Only breathe if the button is wired and not mid-click animation
            if (wired.Contains(btn) && !running.ContainsKey(btn))
            {
                RectTransform rt = btn.GetComponent<RectTransform>();
                if (rt != null)
                {
                    yield return StartCoroutine(BreatheOnce(rt));
                }
            }

            float wait = Random.Range(breatheMinInterval, breatheMaxInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private IEnumerator BreatheOnce(RectTransform rt)
    {
        Vector3 startScale = rt.localScale;
        float half = breatheDuration * 0.5f;

        // Scale up
        float time = 0f;
        while (time < half)
        {
            float t = time / half;
            float smooth = t * t * (3f - 2f * t); // smoothstep
            rt.localScale = startScale * Mathf.Lerp(1f, breatheScale, smooth);
            time += Time.deltaTime;
            yield return null;
        }

        // Scale down
        time = 0f;
        while (time < half)
        {
            float t = time / half;
            float smooth = t * t * (3f - 2f * t);
            rt.localScale = startScale * Mathf.Lerp(breatheScale, 1f, smooth);
            time += Time.deltaTime;
            yield return null;
        }

        rt.localScale = startScale;
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

    // ✅ Called when drag starts - disable ALL buttons to prevent blocking
    // בזמן גרירה: לכבות Raycast + לכבות את קומפוננטת ה-Button
    // רק לכפתורים שמופיעים ב-links (כלומר מנוהלים ע"י SmlAnimManager)
    public void DisableAllButtonsForDrag()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link == null || link.button == null) continue;

            var btn = link.button;

            // לכבות את הכפתור לגמרי (לא משנה alpha / צבע)
            btn.enabled = false;

            // לכבות חסימת Raycast
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = false;

            var graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
                g.raycastTarget = false;
        }
    }

    // בסיום גרירה: להחזיר Button + Raycast רק אם הספוט שלו Settled
    public void RestoreButtonsAfterDrag()
    {
        for (int i = 0; i < links.Count; i++)
        {
            var link = links[i];
            if (link == null || link.spot == null || link.button == null) continue;

            bool enable = link.spot.IsSettled;
            var btn = link.button;

            // הכפתור חוזר לעבוד (קומפוננטה ON)
            btn.enabled = true;

            // רייקאסט רק אם Settled
            CanvasGroup cg = btn.GetComponent<CanvasGroup>();
            if (cg != null) cg.blocksRaycasts = enable;

            var graphics = btn.GetComponentsInChildren<Graphic>(true);
            foreach (var g in graphics)
                g.raycastTarget = enable;
        }
    }
}
