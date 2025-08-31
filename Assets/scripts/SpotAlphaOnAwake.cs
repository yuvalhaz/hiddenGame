using UnityEngine;
using UnityEngine.UI;

public class SpotAlphaOnAwake : MonoBehaviour
{
    [Range(0f,1f)]
    public float alpha = 0.001f; // כמעט שקוף, עדיין נתפס ברייקאסט

    void Awake()
    {
        var g = GetComponent<Graphic>();
        if (g != null)
        {
            var c = g.color;
            c.a = alpha;
            g.color = c;
        }
    }
}
