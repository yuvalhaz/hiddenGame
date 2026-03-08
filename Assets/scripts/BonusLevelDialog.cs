using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Dialog UI for bonus levels - asks the player if they want to watch an ad to unlock a bonus level.
/// Attach this script to your bonus level popup GameObject.
/// Setup: Add a CanvasGroup component, connect "Watch Ad" and "Close" buttons in the Inspector.
/// </summary>
public class BonusLevelDialog : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button watchAdButton;
    [Tooltip("The button the player clicks to watch an ad and unlock the level")]
    [SerializeField] private Button closeButton;
    [Tooltip("The button to close the dialog without watching an ad")]
    [SerializeField] private CanvasGroup dialogGroup;
    [Tooltip("CanvasGroup on this GameObject - used for show/hide")]

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    private AudioSource sfxAudioSource;

    private Action onWatchAdClicked;

    private void Awake()
    {
        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;

        if (dialogGroup == null)
            dialogGroup = GetComponent<CanvasGroup>();

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdPressed);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        HideImmediate();
    }

    private void OnDestroy()
    {
        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(OnWatchAdPressed);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    /// <summary>
    /// Open the dialog. Pass a callback that will be called when the user clicks "Watch Ad".
    /// </summary>
    public void Open(Action onConfirmWatchAd)
    {
        onWatchAdClicked = onConfirmWatchAd;

        if (dialogGroup == null)
        {
            Debug.LogWarning("[BonusLevelDialog] No CanvasGroup found! Trying to activate GameObject directly.");
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlaySound(openSound);
            Debug.Log("[BonusLevelDialog] Opened (without CanvasGroup)");
            return;
        }

        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;
        transform.SetAsLastSibling();

        PlaySound(openSound);
        Debug.Log("[BonusLevelDialog] Opened");
    }

    /// <summary>
    /// Close the dialog without watching an ad.
    /// </summary>
    public void Close()
    {
        PlaySound(closeSound);
        HideImmediate();
        onWatchAdClicked = null;
        Debug.Log("[BonusLevelDialog] Closed");
    }

    public bool IsOpen()
    {
        return dialogGroup != null && dialogGroup.alpha > 0.5f;
    }

    private void OnWatchAdPressed()
    {
        HideImmediate();
        onWatchAdClicked?.Invoke();
        onWatchAdClicked = null;
    }

    private void HideImmediate()
    {
        if (dialogGroup == null)
        {
            gameObject.SetActive(false);
            return;
        }

        dialogGroup.alpha = 0f;
        dialogGroup.interactable = false;
        dialogGroup.blocksRaycasts = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}
