using UnityEngine;

[CreateAssetMenu(fileName = "ItemRevealConfig", menuName = "Game/Item Reveal Config")]
public class ItemRevealConfig : ScriptableObject
{
    [Header("Reveal Animation")]
    public float revealDuration = 0.5f;
    public AnimationCurve revealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Hidden State")]
    public Color hiddenTint = new Color(1f, 1f, 1f, 0f);

    [Header("Reveal Effects")]
    public bool useScalePop = true;
    public float scalePopAmount = 0.15f;
    
    [Header("Audio")]
    public AudioClip revealSound;
}