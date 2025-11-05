using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates a sparkle effect that spreads across the entire revealing area
/// Usage: SparkleBurstEffect.Burst(canvas, targetArea, count, duration);
/// </summary>
public class SparkleBurstEffect : MonoBehaviour
{
    // -------- Static API --------
    /// <summary>
    /// Creates a burst of sparkles across the entire target area
    /// </summary>
    /// <param name="canvas">The canvas to render sparkles on</param>
    /// <param name="targetArea">The RectTransform defining the area to spread sparkles across (null = full canvas)</param>
    /// <param name="count">Number of sparkles to create</param>
    /// <param name="duration">How long sparkles should last</param>
    public static void Burst(Canvas canvas, RectTransform targetArea = null, int count = 50, float duration = 2f)
    {
        if (canvas == null) return;

        // Root container for all sparkles
        var rootGO = new GameObject("SparkleBurst", typeof(RectTransform));
        var rootRT = (RectTransform)rootGO.transform;
        rootRT.SetParent(canvas.transform, false);
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.sizeDelta = Vector2.zero;
        rootRT.anchoredPosition = Vector2.zero;

        // Create and start the sparkle spawner
        var spawner = rootGO.AddComponent<SparkleBurstEffect>();
        spawner.canvas = canvas;
        spawner.targetArea = targetArea ?? (RectTransform)canvas.transform;
        spawner.count = count;
        spawner.duration = duration;
        spawner.Begin();
    }

    // -------- Settings --------
    [Header("Config")]
    public Canvas canvas;
    public RectTransform targetArea;

    [Tooltip("Number of sparkles to create")]
    public int count = 50;

    [Tooltip("Average lifespan of each sparkle")]
    public float duration = 2f;

    [Tooltip("Size range for sparkles (pixels)")]
    public Vector2 sizeRange = new Vector2(10f, 25f);

    [Tooltip("Initial speed range (pixels/sec)")]
    public Vector2 speedRange = new Vector2(100f, 300f);

    [Tooltip("Sparkle gravity (pixels/sec²)")]
    public float gravity = 200f;

    [Tooltip("Air drag (0..1, how much velocity is lost per second)")]
    public float airDrag = 0.95f;

    [Tooltip("Spread factor - how far sparkles spread from spawn point")]
    [Range(0f, 1f)]
    public float spreadFactor = 1f;

    [Header("Visuals")]
    [Tooltip("Sparkle sprite (optional - will create white square if null)")]
    public Sprite sparkleSprite;

    // Sparkle colors - bright and shiny
    public Color32[] palette = new Color32[]
    {
        // Bright whites and yellows (sparkle effect)
        new Color32(255, 255, 255, 255), // Pure White
        new Color32(255, 255, 200, 255), // Warm White
        new Color32(255, 255, 150, 255), // Light Yellow
        new Color32(255, 240, 100, 255), // Golden Yellow

        // Pastels for variety
        new Color32(255, 200, 255, 255), // Light Pink
        new Color32(200, 230, 255, 255), // Light Blue
        new Color32(200, 255, 230, 255), // Light Cyan
        new Color32(255, 255, 220, 255), // Cream
    };

    // -------- Implementation --------
    class Sparkle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float rotSpeed;      // degrees/sec
        public float life;          // total lifespan
        public float age;           // current age
        public Color baseColor;
        public float startAlpha;
        public float pulsePhase;    // for pulsing effect
    }

    readonly List<Sparkle> sparkles = new List<Sparkle>();
    static Sprite cachedSparkleSprite;
    float elapsed;

    public void Begin()
    {
        Debug.Log($"[SparkleBurstEffect] Begin() called - Creating {count} sparkles");

        if (sparkleSprite == null)
            sparkleSprite = GetSparkleSprite();

        CreateSparkles();

        Debug.Log($"[SparkleBurstEffect] Created {sparkles.Count} sparkles successfully");
    }

    static Sprite GetSparkleSprite()
    {
        if (cachedSparkleSprite != null) return cachedSparkleSprite;

        // Create a simple white diamond/star shape
        var tex = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        var pixels = new Color32[]
        {
            new Color32(0, 0, 0, 0),       new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 0),
            new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
            new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255),
            new Color32(0, 0, 0, 0),       new Color32(255, 255, 255, 255), new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 0)
        };
        tex.SetPixels32(pixels);
        tex.Apply(false, false);

        cachedSparkleSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        cachedSparkleSprite.name = "SparkleBurst_Sprite";
        return cachedSparkleSprite;
    }

    void CreateSparkles()
    {
        var rtRoot = (RectTransform)transform;

        // Get the bounds of the target area
        Rect areaBounds = GetAreaBounds();

        Debug.Log($"[SparkleBurstEffect] Creating sparkles in bounds: {areaBounds}");

        for (int i = 0; i < count; i++)
        {
            // Create sparkle GameObject
            var go = new GameObject("sparkle", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rtRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Random position across the entire area
            Vector2 randomPos = new Vector2(
                Random.Range(areaBounds.xMin, areaBounds.xMax),
                Random.Range(areaBounds.yMin, areaBounds.yMax)
            );
            rt.anchoredPosition = randomPos;

            var img = go.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            // Random sparkle color
            var baseCol = (Color)palette[Random.Range(0, palette.Length)];
            img.color = baseCol;

            // Random size
            float size = Random.Range(sizeRange.x, sizeRange.y);
            rt.sizeDelta = new Vector2(size, size);

            // Make sure sparkle renders on top
            rt.SetAsLastSibling();

            // Random velocity - mostly upward and outward
            Vector2 dir = Random.insideUnitCircle.normalized;
            dir.y = Mathf.Abs(dir.y) * Random.Range(0.5f, 1f) + 0.3f; // Bias upward
            dir.Normalize();
            float spd = Random.Range(speedRange.x, speedRange.y);

            float rotSpeed = Random.Range(-720f, 720f); // Fast rotation for sparkle effect
            float life = duration * Random.Range(0.8f, 1.3f);

            // Set Canvas Group alpha to ensure visibility
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            sparkles.Add(new Sparkle
            {
                rt = rt,
                img = img,
                vel = dir * spd * spreadFactor,
                rotSpeed = rotSpeed,
                life = life,
                age = 0f,
                baseColor = baseCol,
                startAlpha = 1f,
                pulsePhase = Random.Range(0f, Mathf.PI * 2f)
            });
        }

        Debug.Log($"[SparkleBurstEffect] All {sparkles.Count} sparkles created and positioned");
    }

    Rect GetAreaBounds()
    {
        if (targetArea == null || canvas == null)
        {
            // Default to full canvas
            RectTransform canvasRT = canvas != null ? canvas.GetComponent<RectTransform>() : null;
            if (canvasRT != null)
            {
                Rect canvasRect = canvasRT.rect;
                return new Rect(0, 0, canvasRect.width, canvasRect.height);
            }
            return new Rect(0, 0, Screen.width, Screen.height);
        }

        // Convert target area's world corners to canvas local space
        Vector3[] worldCorners = new Vector3[4];
        targetArea.GetWorldCorners(worldCorners);

        RectTransform canvasRT2 = canvas.GetComponent<RectTransform>();
        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;

        for (int i = 0; i < 4; i++)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRT2,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, worldCorners[i]),
                canvas.worldCamera,
                out localPoint
            );

            min = Vector2.Min(min, localPoint);
            max = Vector2.Max(max, localPoint);
        }

        // Convert to canvas space (0,0 at bottom-left)
        Rect canvasRect2 = canvasRT2.rect;
        Vector2 offset = new Vector2(canvasRect2.width / 2f, canvasRect2.height / 2f);

        return new Rect(
            min.x + offset.x,
            min.y + offset.y,
            max.x - min.x,
            max.y - min.y
        );
    }

    void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        for (int i = sparkles.Count - 1; i >= 0; i--)
        {
            var s = sparkles[i];
            s.age += dt;

            // Physics
            s.vel += Vector2.down * gravity * dt;
            s.vel *= Mathf.Pow(airDrag, dt);
            s.rt.anchoredPosition += s.vel * dt;

            // Rotation
            var e = s.rt.localEulerAngles;
            e.z += s.rotSpeed * dt;
            s.rt.localEulerAngles = e;

            // Pulsing sparkle effect
            float t = Mathf.Clamp01(s.age / s.life);
            s.pulsePhase += dt * 8f; // Pulse speed
            float pulse = (Mathf.Sin(s.pulsePhase) + 1f) * 0.5f; // 0..1

            // Fade out with age
            float alpha = Mathf.Lerp(s.startAlpha, 0f, Mathf.SmoothStep(0f, 1f, t));

            // Apply pulsing to alpha and color brightness
            alpha *= Mathf.Lerp(0.5f, 1f, pulse);
            Color brightColor = Color.Lerp(s.baseColor, Color.white, pulse * 0.5f);

            s.img.color = new Color(brightColor.r, brightColor.g, brightColor.b, alpha);

            // Subtle scale variation
            float scale = Mathf.Lerp(1f, 0.4f, t) * Mathf.Lerp(0.8f, 1.2f, pulse);
            s.rt.localScale = new Vector3(scale, scale, 1f);

            // Remove if expired
            if (s.age >= s.life)
            {
                Destroy(s.rt.gameObject);
                sparkles.RemoveAt(i);
            }
        }

        // Destroy self when all sparkles are gone
        if (sparkles.Count == 0)
            Destroy(gameObject);
    }
}
