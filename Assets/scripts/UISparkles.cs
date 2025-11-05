using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// אפקט ניצנוצים (sparkles) מבוסס UI
/// נוצר מעל תמונה כאשר השחקן מניח אותה במקום הנכון
/// קריאה מהירה: UISparkles.Burst(canvas, targetRect);
/// </summary>
public class UISparkles : MonoBehaviour
{
    // -------- API סטטי --------
    public static void Burst(Canvas canvas, RectTransform target, int count = 50, float duration = 1.0f)
    {
        if (canvas == null || target == null) return;

        // Root מתחת ל-Canvas
        var rootGO = new GameObject("UISparklesBurst", typeof(RectTransform));
        var rootRT = (RectTransform)rootGO.transform;
        rootRT.SetParent(canvas.transform, false);
        rootRT.anchorMin = rootRT.anchorMax = new Vector2(0.5f, 0.5f);
        rootRT.pivot = new Vector2(0.5f, 0.5f);

        // מיקום root במרכז היעד
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)canvas.transform,
            RectTransformUtility.WorldToScreenPoint(cam, worldCenter),
            cam,
            out localPoint
        );
        rootRT.anchoredPosition = localPoint;

        // יצירת הספונר והפעלה
        var spawner = rootGO.AddComponent<UISparkles>();
        spawner.canvas = canvas;
        spawner.count = count;
        spawner.duration = duration;
        spawner.targetSize = target.rect.size;
        spawner.Begin();
    }

    // -------- הגדרות --------
    [Header("Config")]
    public Canvas canvas;
    [Tooltip("כמה ניצנוצים ליצור")]
    public int count = 50;
    [Tooltip("משך חיים ממוצע (לניצוץ אחד)")]
    public float duration = 1.0f;
    [Tooltip("טווח גדלים (פיקסלים)")]
    public Vector2 sizePxRange = new Vector2(4f, 12f);
    [Tooltip("טווח מהירויות (פיקסלים/שניה)")]
    public Vector2 speedPxPerSec = new Vector2(100f, 300f);
    [Tooltip("האם ניצנוצים נופלים למטה")]
    public bool useGravity = false;
    [Tooltip("כבידה בפיקסלים/שניה^2")]
    public float gravityPx = 300f;

    [Header("Visuals")]
    [Tooltip("ספרייט לניצנוצים (אם ריק - נוצר עיגול לבן)")]
    public Sprite sparkleSprite;

    // פלטת צבעים מנצנצים: זהב, כסף, לבן, צהוב
    public Color32[] palette = new Color32[]
    {
        new Color32(255, 223, 0, 255),   // זהב בהיר
        new Color32(255, 215, 0, 255),   // זהב
        new Color32(255, 255, 255, 255), // לבן
        new Color32(255, 255, 200, 255), // צהוב בהיר
        new Color32(230, 230, 250, 255), // כסף בהיר
        new Color32(192, 192, 192, 255), // כסף
        new Color32(255, 250, 205, 255), // לימון בהיר
        new Color32(255, 239, 213, 255)  // שמנת
    };

    private Vector2 targetSize;

    // -------- מימוש --------
    class Sparkle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float angVel;      // deg/sec
        public float life;        // משך חיים
        public float age;         // זמן שעבר
        public Color baseColor;   // צבע בסיס
        public float startAlpha;  // אלפא התחלתי
        public float pulseSpeed;  // מהירות הבהוב
    }

    readonly List<Sparkle> sparkles = new List<Sparkle>();
    static Sprite cachedCircleSprite;

    public void Begin()
    {
        if (sparkleSprite == null)
            sparkleSprite = GetCircleSprite();

        CreateSparkles();
    }

    static Sprite GetCircleSprite()
    {
        if (cachedCircleSprite != null) return cachedCircleSprite;

        // טקסטורה 16x16 עם עיגול
        int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var pixels = new Color32[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = dist < radius ? (byte)255 : (byte)0;

                // gradient מהמרכז החוצה
                float normalizedDist = dist / radius;
                alpha = (byte)(255 * Mathf.Clamp01(1f - normalizedDist));

                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);

        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        cachedCircleSprite.name = "UISparkles_Circle";
        return cachedCircleSprite;
    }

    void CreateSparkles()
    {
        var rtRoot = (RectTransform)transform;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("sparkle", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rtRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // התחלה ממיקום אקראי בתוך גבולות התמונה
            float startX = Random.Range(-targetSize.x * 0.4f, targetSize.x * 0.4f);
            float startY = Random.Range(-targetSize.y * 0.4f, targetSize.y * 0.4f);
            rt.anchoredPosition = new Vector2(startX, startY);

            var img = go.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.type = Image.Type.Simple;

            // צבע מנצנץ מהפלטה
            var baseCol = (Color)palette[Random.Range(0, palette.Length)];
            img.color = baseCol;

            float size = Random.Range(sizePxRange.x, sizePxRange.y);
            rt.sizeDelta = new Vector2(size, size);

            // מהירות התחלתית - מתפרץ מהמרכז החוצה
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.magnitude < 0.1f) dir = Vector2.up;

            float spd = Random.Range(speedPxPerSec.x, speedPxPerSec.y);

            float angVel = Random.Range(-720f, 720f); // סיבוב מהיר
            float life = duration * Random.Range(0.8f, 1.2f);

            sparkles.Add(new Sparkle
            {
                rt = rt,
                img = img,
                vel = dir * spd,
                angVel = angVel,
                life = life,
                age = 0f,
                baseColor = baseCol,
                startAlpha = 1f,
                pulseSpeed = Random.Range(5f, 10f)
            });
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        for (int i = sparkles.Count - 1; i >= 0; i--)
        {
            var p = sparkles[i];
            p.age += dt;

            // תנועה
            if (useGravity)
            {
                p.vel += Vector2.down * gravityPx * dt;
            }

            // האטה הדרגתית
            p.vel *= Mathf.Pow(0.92f, dt);
            p.rt.anchoredPosition += p.vel * dt;

            // סיבוב
            var e = p.rt.localEulerAngles;
            e.z += p.angVel * dt;
            p.rt.localEulerAngles = e;

            // fade out + הבהוב (twinkle)
            float t = Mathf.Clamp01(p.age / p.life);

            // אלפא יורד עם הזמן
            float baseAlpha = Mathf.Lerp(p.startAlpha, 0f, t * t);

            // הבהוב (pulse)
            float pulse = Mathf.Sin(p.age * p.pulseSpeed) * 0.3f + 0.7f;

            float finalAlpha = baseAlpha * pulse;

            p.img.color = new Color(p.baseColor.r, p.baseColor.g, p.baseColor.b, finalAlpha);

            // התכווצות עדינה
            float s = Mathf.Lerp(1f, 0.3f, t);
            p.rt.localScale = new Vector3(s, s, 1f);

            if (p.age >= p.life)
            {
                Destroy(p.rt.gameObject);
                sparkles.RemoveAt(i);
            }
        }

        if (sparkles.Count == 0)
            Destroy(gameObject);
    }
}
