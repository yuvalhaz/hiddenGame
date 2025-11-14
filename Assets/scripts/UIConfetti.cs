using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// אפקט קונפטי מבוסס UI (בלי Particle System).
/// עובד בכל מצב Canvas, כולל Screen Space - Overlay.
/// קריאה מהירה: UIConfetti.Burst(canvas, targetRect, count, duration);
/// </summary>
public class UIConfetti : MonoBehaviour
{
    // -------- API סטטי --------
    public static void Burst(Canvas canvas, RectTransform target, int count = 100, float duration = 1.2f)
    {
        if (canvas == null || target == null) return;

        // Root מתחת ל-Canvas
        var rootGO = new GameObject("UIConfettiBurst", typeof(RectTransform));
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
        var spawner = rootGO.AddComponent<UIConfetti>();
        spawner.canvas = canvas;
        spawner.count = count;
        spawner.duration = duration;
        spawner.Begin();
    }

    // -------- הגדרות --------
    [Header("Config")]
    public Canvas canvas;
    [Tooltip("כמה חתיכות קונפטי ליצור בבורסט")]
    public int count = 100;
    [Tooltip("משך חיים ממוצע (לחתיכה אחת)")]
    public float duration = 1.2f;
    [Tooltip("טווח גדלים (פיקסלים)")]
    public Vector2 sizePxRange = new Vector2(8f, 18f);
    [Tooltip("טווח מהירויות התחלתיות (פיקסלים/שניה)")]
    public Vector2 speedPxPerSec = new Vector2(250f, 600f);
    [Tooltip("כבידה בפיקסלים/שניה^2")]
    public float gravityPx = 1000f;
    [Tooltip("דראג אוויר (0..1, כמה מאבדים מהירות בכל שניה)")]
    public float airDrag = 0.9f;

    [Header("Visuals")]
    [Tooltip("ספרייט לחתיכות הקונפטי (לא חובה). אם ריק – נוצר ספרייט לבן קטן בזיכרון.")]
    public Sprite pieceSprite;

    // פלטת פסטלים: ירוק / תכלת / ורוד
    public Color32[] palette = new Color32[]
    {
        // Greens (Pastel)
        new Color32(0xA8,0xE6,0xCF,255), // Mint Green
        new Color32(0xC7,0xF9,0xCC,255), // Soft Pastel Green

        // Teals / Light Blues
        new Color32(0xA0,0xE7,0xE5,255), // Light Turquoise
        new Color32(0xB2,0xF7,0xEF,255), // Pastel Teal
        new Color32(0xBD,0xE0,0xFE,255), // Baby Blue

        // Pinks (Pastel)
        new Color32(0xFF,0xC6,0xFF,255), // Pastel Pink
        new Color32(0xFF,0xD1,0xDC,255), // Baby Pink
        new Color32(0xFF,0xE5,0xEC,255)  // Blush
    };

    // -------- מימוש --------
    class Piece
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float angVel;      // deg/sec
        public float life;        // משך חיים
        public float age;         // זמן שעבר
        public Color baseColor;   // צבע בסיס (בלי שינויי פייד)
        public float startAlpha;  // אלפא התחלתי
    }

    readonly List<Piece> pieces = new List<Piece>();
    static Sprite cachedWhiteSprite;
    float elapsed;

    public void Begin()
    {
        if (pieceSprite == null)
            pieceSprite = GetWhiteSprite();

        CreatePieces();
    }

    static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;

        // טקסטורה 2x2 לבנה
        var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        var px = new Color32[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels32(px);
        tex.Apply(false, true); // ✅ makeNoLongerReadable=true to free CPU memory and prevent TLS leak

        cachedWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f);
        cachedWhiteSprite.name = "UIConfetti_White";
        return cachedWhiteSprite;
    }

    void CreatePieces()
    {
        var rtRoot = (RectTransform)transform;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("confetti", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(rtRoot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var img = go.GetComponent<Image>();
            img.sprite = pieceSprite;
            img.type = Image.Type.Simple;

            // צבע בסיס מהפלטה
            var baseCol = (Color)palette[Random.Range(0, palette.Length)];
            img.color = baseCol;

            float size = Random.Range(sizePxRange.x, sizePxRange.y);
            rt.sizeDelta = new Vector2(size, size * Random.Range(0.7f, 1.3f));

            // מהירות התחלתית אקראית, נטייה למעלה
            Vector2 dir = Random.insideUnitCircle.normalized;
            dir.y = Mathf.Abs(dir.y) * Random.Range(0.6f, 1f) + 0.2f;
            dir.Normalize();
            float spd = Random.Range(speedPxPerSec.x, speedPxPerSec.y);

            float angVel = Random.Range(-360f, 360f);
            float life = duration * Random.Range(0.9f, 1.2f);

            pieces.Add(new Piece
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

        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            var p = pieces[i];
            p.age += dt;

            // "פיזיקה" פשוטה בפיקסלים
            p.vel += Vector2.down * gravityPx * dt;
            p.vel *= Mathf.Pow(airDrag, dt);
            p.rt.anchoredPosition += p.vel * dt;

            // סיבוב
            var e = p.rt.localEulerAngles;
            e.z += p.angVel * dt;
            p.rt.localEulerAngles = e;

            // פייד + ריכוך פסטלי קבוע (לכיוון לבן)
            float t = Mathf.Clamp01(p.age / p.life);
            float alpha = Mathf.Lerp(p.startAlpha, 0f, Mathf.SmoothStep(0f, 1f, t));
            Color pastel = Color.Lerp(p.baseColor, Color.white, 0.25f); // 25% לכיוון לבן
            p.img.color = new Color(pastel.r, pastel.g, pastel.b, alpha);

            // שינוי קנה מידה עדין לאורך החיים
            float s = Mathf.Lerp(1f, 0.6f, t);
            p.rt.localScale = new Vector3(s, s, 1f);

            if (p.age >= p.life)
            {
                Destroy(p.rt.gameObject);
                pieces.RemoveAt(i);
            }
        }

        if (pieces.Count == 0)
            Destroy(gameObject);
    }
}
