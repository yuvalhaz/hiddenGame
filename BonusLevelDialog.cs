using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dialog UI for bonus levels - asks player to watch a rewarded ad to unlock a bonus level.
/// Shows "Watch Ad" and "Close" buttons. Calls onConfirm callback when user agrees.
/// </summary>
public class BonusLevelDialog : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup dialogGroup;
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip openDialogSound;
    [SerializeField] private AudioClip closeDialogSound;
    private AudioSource sfxAudioSource;

    private Action onConfirmCallback;
    private bool isClosing = false;

    private void Awake()
    {
        sfxAudioSource = gameObject.AddComponent<AudioSource>();
        sfxAudioSource.playOnAwake = false;

        if (dialogGroup == null)
            dialogGroup = GetComponent<CanvasGroup>();

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAdClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(OnWatchAdClicked);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    /// <summary>
    /// Check if the dialog is currently visible
    /// </summary>
    public bool IsOpen()
    {
        return dialogGroup != null && dialogGroup.alpha > 0.5f;
    }

    /// <summary>
    /// Opens the bonus level dialog. When user clicks "Watch Ad", onConfirm is invoked.
    /// </summary>
    public void Open(Action onConfirm)
    {
        onConfirmCallback = onConfirm;

        ShowImmediate();
        transform.SetAsLastSibling();

        PlaySound(openDialogSound);

        Debug.Log("[BonusLevelDialog] Opened");
    }

    /// <summary>
    /// Closes the dialog without triggering the ad.
    /// </summary>
    public void Close()
    {
        if (isClosing) return;

        isClosing = true;

        PlaySound(closeDialogSound);

        HideImmediate();
        onConfirmCallback = null;

        isClosing = false;

        Debug.Log("[BonusLevelDialog] Closed");
    }

    /// <summary>
    /// Called when user clicks "Watch Ad" button.
    /// </summary>
    private void OnWatchAdClicked()
    {
        var callback = onConfirmCallback;
        HideImmediate();
        onConfirmCallback = null;

        callback?.Invoke();
    }

    private void ShowImmediate()
    {
        if (dialogGroup == null) return;

        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (dialogGroup == null) return;

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
