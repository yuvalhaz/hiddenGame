using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// אפקט נצנצים מהבהבים (sparkles) - אפקט סטטי שמהבהב במקום
/// מחליף בין + ל-* כמו אפקט חשיבה/נצנוץ
/// קריאה מהירה: UISparkles.Burst(canvas, targetRect, count, duration);
/// </summary>
public class UISparkles : MonoBehaviour
{
    // -------- API סטטי --------
    public static void Burst(Canvas canvas, RectTransform target, int count = 8, float duration = 1.0f)
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
    [Tooltip("כמה נצנצים ליצור")]
    public int count = 8;
    [Tooltip("משך חיים כולל")]
    public float duration = 1.0f;
    [Tooltip("גודל היעד - לפיזור הנצנצים")]
    public Vector2 targetSize = new Vector2(100f, 100f);
    [Tooltip("טווח גדלים (פיקסלים)")]
    public Vector2 sizeRange = new Vector2(20f, 40f);
    [Tooltip("מהירות הבהוב (פעמים לשניה)")]
    public float blinkSpeed = 8f;

    // פלטת צבעים זהב/צהוב בהיר
    public Color32[] palette = new Color32[]
    {
        new Color32(0xFF,0xD7,0x00,255), // Gold
        new Color32(0xFF,0xE5,0x6D,255), // Light Gold
        new Color32(0xFF,0xFF,0x00,255), // Yellow
        new Color32(0xFF,0xF4,0x94,255), // Pale Gold
    };

    // -------- מימוש --------
    class Sparkle
    {
        public RectTransform rt;
        public Image img;
        public float life;
        public float age;
        public Color baseColor;
        public float blinkOffset; // קיזוז אקראי להבהוב
    }

    readonly List<Sparkle> sparkles = new List<Sparkle>();
    static Sprite cachedPlusSprite;
    static Sprite cachedStarSprite;
    float elapsed;

    public void Begin()
    {
        if (cachedPlusSprite == null) cachedPlusSprite = CreatePlusSprite();
        if (cachedStarSprite == null) cachedStarSprite = CreateStarSprite();
        CreateSparkles();
    }

    // יצירת ספרייט של +
    static Sprite CreatePlusSprite()
    {
        int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var pixels = new Color32[size * size];

        // התחל עם שקוף
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 0);

        // צייר + (קו אנכי וקו אפקי)
        int center = size / 2;
        int thickness = 3;

        for (int y = 0; y < size; y++)
        {
            for (int x = center - thickness / 2; x <= center + thickness / 2; x++)
            {
                pixels[y * size + x] = Color.white; // קו אנכי
            }
        }

        for (int x = 0; x < size; x++)
        {
            for (int y = center - thickness / 2; y <= center + thickness / 2; y++)
            {
                pixels[y * size + x] = Color.white; // קו אפקי
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        tex.filterMode = FilterMode.Bilinear;

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "UISparkles_Plus";
        return sprite;
    }

    // יצירת ספרייט של *
    static Sprite CreateStarSprite()
    {
        int size = 16;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var pixels = new Color32[size * size];

        // התחל עם שקוף
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 0);

        int center = size / 2;

        // צייר * (קווים באלכסונים + אנכי/אפקי)
        for (int i = 0; i < size; i++)
        {
            // אלכסון \
            pixels[i * size + i] = Color.white;
            if (i > 0) pixels[i * size + (i - 1)] = Color.white;
            if (i < size - 1) pixels[i * size + (i + 1)] = Color.white;

            // אלכסון /
            pixels[i * size + (size - 1 - i)] = Color.white;
            if (i > 0) pixels[i * size + (size - i)] = Color.white;
            if (i < size - 1) pixels[i * size + (size - 2 - i)] = Color.white;

            // קו אנכי
            pixels[i * size + center] = Color.white;
            // קו אפקי
            pixels[center * size + i] = Color.white;
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        tex.filterMode = FilterMode.Bilinear;

        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = "UISparkles_Star";
        return sprite;
    }

    void CreateSparkles()
    {
        var rtRoot = (RectTransform)transform;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("sparkle", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rtRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            // מקם את הנצנץ באופן אקראי סביב היעד
            float radius = Mathf.Max(targetSize.x, targetSize.y) * 0.5f;
            Vector2 randomPos = Random.insideUnitCircle * radius * 0.8f;
            rt.anchoredPosition = randomPos;

            var img = go.GetComponent<Image>();
            img.sprite = cachedPlusSprite; // מתחיל עם +
            img.type = Image.Type.Simple;
            img.raycastTarget = false;

            // צבע אקראי מהפלטה
            var baseCol = (Color)palette[Random.Range(0, palette.Length)];
            img.color = baseCol;

            float size = Random.Range(sizeRange.x, sizeRange.y);
            rt.sizeDelta = new Vector2(size, size);

            float life = duration * Random.Range(0.9f, 1.1f);
            float blinkOffset = Random.Range(0f, 1f); // כל נצנץ מתחיל בזמן שונה

            sparkles.Add(new Sparkle
            {
                rt = rt,
                img = img,
                life = life,
                age = 0f,
                baseColor = baseCol,
                blinkOffset = blinkOffset
            });
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        for (int i = sparkles.Count - 1; i >= 0; i--)
        {
            var s = sparkles[i];
            s.age += dt;

            // חישוב אם להציג + או *
            float blinkTime = (s.age + s.blinkOffset) * blinkSpeed;
            bool showPlus = (Mathf.FloorToInt(blinkTime) % 2) == 0;
            s.img.sprite = showPlus ? cachedPlusSprite : cachedStarSprite;

            // פייד אאוט בסוף
            float t = Mathf.Clamp01(s.age / s.life);
            float alpha;

            if (t < 0.2f)
            {
                // fade in מהיר
                alpha = t / 0.2f;
            }
            else if (t > 0.8f)
            {
                // fade out מהיר בסוף
                alpha = (1f - t) / 0.2f;
            }
            else
            {
                // מלא באמצע
                alpha = 1f;
            }

            s.img.color = new Color(s.baseColor.r, s.baseColor.g, s.baseColor.b, alpha);

            if (s.age >= s.life)
            {
                Destroy(s.rt.gameObject);
                sparkles.RemoveAt(i);
            }
        }

        if (sparkles.Count == 0)
            Destroy(gameObject);
    }
}
