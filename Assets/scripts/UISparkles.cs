using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// אפקט נצנצים עדין (sparkles) - אפקט קטן וממוקד לפעולות בודדות
/// שונה מ-UIConfetti - פחות חלקיקים, יותר מהיר, יותר עדין
/// קריאה מהירה: UISparkles.Burst(canvas, targetRect, count, duration);
/// </summary>
public class UISparkles : MonoBehaviour
{
    // -------- API סטטי --------
    public static void Burst(Canvas canvas, RectTransform target, int count = 20, float duration = 0.8f)
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
        spawner.Begin();
    }

    // -------- הגדרות --------
    [Header("Config")]
    public Canvas canvas;
    [Tooltip("כמה נצנצים ליצור")]
    public int count = 20;
    [Tooltip("משך חיים ממוצע (קצר יותר מקונפטי)")]
    public float duration = 0.8f;
    [Tooltip("טווח גדלים (פיקסלים) - קטנים יותר")]
    public Vector2 sizePxRange = new Vector2(4f, 10f);
    [Tooltip("טווח מהירויות התחלתיות (פיקסלים/שניה)")]
    public Vector2 speedPxPerSec = new Vector2(150f, 350f);
    [Tooltip("כבידה בפיקסלים/שניה^2")]
    public float gravityPx = 400f;
    [Tooltip("דראג אוויר")]
    public float airDrag = 0.95f;

    [Header("Visuals")]
    [Tooltip("ספרייט לנצנצים (לא חובה)")]
    public Sprite sparkleSprite;

    // פלטת זהב/כסף/כוכבים מנצנצים
    public Color32[] palette = new Color32[]
    {
        new Color32(0xFF,0xF4,0x94,255), // Gold
        new Color32(0xFF,0xE5,0x6D,255), // Light Gold
        new Color32(0xFF,0xD7,0x00,255), // Bright Gold
        new Color32(0xFF,0xFF,0xFF,255), // White
        new Color32(0xF0,0xF0,0xF0,255), // Silver
        new Color32(0xFF,0xEC,0xB3,255), // Pale Gold
    };

    // -------- מימוש --------
    class Sparkle
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float angVel;
        public float life;
        public float age;
        public Color baseColor;
        public float startAlpha;
    }

    readonly List<Sparkle> sparkles = new List<Sparkle>();
    static Sprite cachedStarSprite;
    float elapsed;

    public void Begin()
    {
        if (sparkleSprite == null)
            sparkleSprite = GetStarSprite();

        CreateSparkles();
    }

    static Sprite GetStarSprite()
    {
        if (cachedStarSprite != null) return cachedStarSprite;

        // טקסטורה 4x4 בצורת יהלום/כוכב פשוט
        var tex = new Texture2D(4, 4, TextureFormat.ARGB32, false);
        var px = new Color32[16];

        // יהלום פשוט
        for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 0);

        // מרכז
        px[5] = px[6] = px[9] = px[10] = Color.white;
        // קצוות
        px[1] = px[2] = px[4] = px[7] = px[8] = px[11] = px[13] = px[14] = new Color32(255, 255, 255, 180);

        tex.SetPixels32(px);
        tex.Apply(false, false);

        cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        cachedStarSprite.name = "UISparkles_Star";
        return cachedStarSprite;
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
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.type = Image.Type.Simple;

            // צבע מנצנץ מהפלטה
            var baseCol = (Color)palette[Random.Range(0, palette.Length)];
            img.color = baseCol;

            float size = Random.Range(sizePxRange.x, sizePxRange.y);
            rt.sizeDelta = new Vector2(size, size); // ריבוע/יהלום

            // מהירות התחלתית - פיזור בכל הכיוונים
            Vector2 dir = Random.insideUnitCircle.normalized;
            float spd = Random.Range(speedPxPerSec.x, speedPxPerSec.y);

            float angVel = Random.Range(-720f, 720f); // סיבוב מהיר יותר
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
                startAlpha = 1f
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

            // פיזיקה קלה
            s.vel += Vector2.down * gravityPx * dt;
            s.vel *= Mathf.Pow(airDrag, dt);
            s.rt.anchoredPosition += s.vel * dt;

            // סיבוב מהיר
            var e = s.rt.localEulerAngles;
            e.z += s.angVel * dt;
            s.rt.localEulerAngles = e;

            // פייד אאוט חד + נצנוץ
            float t = Mathf.Clamp01(s.age / s.life);
            float alpha = Mathf.Lerp(s.startAlpha, 0f, t * t); // פייד מהיר

            // אפקט נצנוץ עדין
            float twinkle = 1f + Mathf.Sin(s.age * 15f) * 0.3f;
            alpha *= twinkle;

            s.img.color = new Color(s.baseColor.r, s.baseColor.g, s.baseColor.b, alpha);

            // קנה מידה הולך וקטן
            float scale = Mathf.Lerp(1f, 0.3f, t);
            s.rt.localScale = new Vector3(scale, scale, 1f);

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
