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
    public static void Burst(Canvas canvas, RectTransform target, int count = 100, float duration = 1.2f, AudioClip sfx = null, AudioSource audioSource = null, float volume = 1f, Color32[] customPalette = null)
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
        spawner.confettiSound = sfx;
        spawner.confettiAudioSource = audioSource;
        spawner.confettiVolume = volume;

        // אם יש פלטת צבעים מותאמת אישית, השתמש בה
        if (customPalette != null && customPalette.Length > 0)
        {
            spawner.palette = customPalette;
        }

        spawner.Begin();
    }

    // -------- הגדרות --------
    [Header("Config")]
    public Canvas canvas;
    [Tooltip("כמה חתיכות קונפטי ליצור בבורסט")]
    public int count = 100;
    [Tooltip("משך חיים ממוצע (לחתיכה אחת)")]
    public float duration = 1.2f;

    [Header("Audio")]
    [Tooltip("סאונד לבורסט קונפטי")]
    public AudioClip confettiSound;
    [Tooltip("AudioSource להשמעת הסאונד")]
    public AudioSource confettiAudioSource;
    [Tooltip("עוצמת הסאונד")]
    public float confettiVolume = 1f;
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

    // פלטת צבעים תוססת ועשירה יותר
    public Color32[] palette = new Color32[]
    {
        // Vibrant Greens
        new Color32(0x00,0xFF,0x7F,255), // Spring Green
        new Color32(0x32,0xCD,0x32,255), // Lime Green
        new Color32(0x7F,0xFF,0x00,255), // Chartreuse

        // Vibrant Blues & Teals
        new Color32(0x00,0xBF,0xFF,255), // Deep Sky Blue
        new Color32(0x1E,0x90,0xFF,255), // Dodger Blue
        new Color32(0x00,0xCE,0xD1,255), // Dark Turquoise
        new Color32(0x48,0xD1,0xCC,255), // Medium Turquoise

        // Vibrant Pinks & Purples
        new Color32(0xFF,0x14,0x93,255), // Deep Pink
        new Color32(0xFF,0x69,0xB4,255), // Hot Pink
        new Color32(0xDA,0x70,0xD6,255), // Orchid
        new Color32(0xBA,0x55,0xD3,255), // Medium Orchid

        // Vibrant Yellows & Oranges
        new Color32(0xFF,0xD7,0x00,255), // Gold
        new Color32(0xFF,0xA5,0x00,255), // Orange
        new Color32(0xFF,0x8C,0x00,255), // Dark Orange
        new Color32(0xFF,0x69,0x00,255), // Orange Red

        // Vibrant Reds
        new Color32(0xFF,0x00,0x00,255), // Red
        new Color32(0xFF,0x45,0x00,255), // Orange Red
        new Color32(0xFF,0x14,0x93,255)  // Deep Pink
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
        public float pulsePhase;  // פאזה של אפקט נצנוץ
        public float wobblePhase; // פאזה של תנודה
        public Vector2 wobbleAxis; // כיוון התנודה
    }

    readonly List<Piece> pieces = new List<Piece>();
    static Sprite cachedWhiteSprite;
    float elapsed;

    public void Begin()
    {
        Debug.Log($"[UIConfetti] Begin() - Creating {count} confetti pieces");

        // השמעת סאונד קונפטי
        if (confettiSound != null && confettiAudioSource != null)
        {
            confettiAudioSource.PlayOneShot(confettiSound, confettiVolume);
            Debug.Log($"[UIConfetti] Playing confetti sound at volume {confettiVolume}");
        }

        if (pieceSprite == null)
            pieceSprite = GetWhiteSprite();

        CreatePieces();

        Debug.Log($"[UIConfetti] Created {pieces.Count} confetti pieces successfully");
    }

    static Sprite GetWhiteSprite()
    {
        if (cachedWhiteSprite != null) return cachedWhiteSprite;

        // טקסטורה 2x2 לבנה
        var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
        var px = new Color32[] { Color.white, Color.white, Color.white, Color.white };
        tex.SetPixels32(px);
        tex.Apply(false, false);

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
            img.raycastTarget = false;

            // צבע בסיס מהפלטה - תוסס ועז!
            var baseCol = (Color)palette[Random.Range(0, palette.Length)];
            img.color = baseCol;

            float size = Random.Range(sizePxRange.x, sizePxRange.y);
            // קונפטי בצורות שונות - מלבנים ארוכים יותר
            float aspectRatio = Random.Range(0.4f, 1.8f);
            rt.sizeDelta = new Vector2(size, size * aspectRatio);

            // CanvasGroup לשליטה באלפא
            var cg = go.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            // Render on top
            rt.SetAsLastSibling();

            // מהירות התחלתית אקראית, נטייה למעלה עם יותר וריאציה
            Vector2 dir = Random.insideUnitCircle.normalized;
            dir.y = Mathf.Abs(dir.y) * Random.Range(0.5f, 1f) + 0.3f;
            dir.Normalize();
            float spd = Random.Range(speedPxPerSec.x, speedPxPerSec.y);

            // סיבוב מהיר יותר לאפקט דרמטי
            float angVel = Random.Range(-540f, 540f);
            float life = duration * Random.Range(0.8f, 1.3f);

            pieces.Add(new Piece
            {
                rt = rt,
                img = img,
                vel = dir * spd,
                angVel = angVel,
                life = life,
                age = 0f,
                baseColor = baseCol,
                startAlpha = 1f,
                pulsePhase = Random.Range(0f, Mathf.PI * 2f),
                wobblePhase = Random.Range(0f, Mathf.PI * 2f),
                wobbleAxis = Random.insideUnitCircle.normalized
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

            // "פיזיקה" משופרת עם תנודות
            p.vel += Vector2.down * gravityPx * dt;
            p.vel *= Mathf.Pow(airDrag, dt);

            // תנודה אופקית (wobble) לאפקט טבעי יותר
            p.wobblePhase += dt * 3f;
            Vector2 wobble = p.wobbleAxis * Mathf.Sin(p.wobblePhase) * 20f;

            p.rt.anchoredPosition += (p.vel * dt) + (wobble * dt);

            // סיבוב עם האטה הדרגתית
            float t = Mathf.Clamp01(p.age / p.life);
            float angVelDamped = p.angVel * (1f - t * 0.3f); // האטה ב-30%
            var e = p.rt.localEulerAngles;
            e.z += angVelDamped * dt;
            p.rt.localEulerAngles = e;

            // ✨ אפקט נצנוץ (pulse) כמו ספרקלס
            p.pulsePhase += dt * 6f;
            float pulse = (Mathf.Sin(p.pulsePhase) + 1f) * 0.5f; // 0..1

            // פייד עם נצנוץ
            float alpha = Mathf.Lerp(p.startAlpha, 0f, Mathf.SmoothStep(0f, 1f, t));
            alpha *= Mathf.Lerp(0.7f, 1f, pulse); // נצנוץ עדין

            // 🎨 צבע דינמי - נצנוץ לכיוון בהיר יותר
            Color vibrantColor = Color.Lerp(p.baseColor, Color.white, pulse * 0.3f);
            p.img.color = new Color(vibrantColor.r, vibrantColor.g, vibrantColor.b, alpha);

            // שינוי קנה מידה עם נצנוץ עדין
            float baseScale = Mathf.Lerp(1f, 0.5f, t);
            float pulseScale = Mathf.Lerp(0.9f, 1.1f, pulse);
            float s = baseScale * pulseScale;
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
