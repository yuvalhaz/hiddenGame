using UnityEngine;

/// <summary>
/// Manages background music across all scenes.
/// Singleton pattern - persists between scenes.
/// </summary>
public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    public static MusicManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("MusicManager");
                instance = go.AddComponent<MusicManager>();
            }
            return instance;
        }
    }

    [Header("Music Settings")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private float volume = 0.5f;

    private AudioSource audioSource;
    private bool isMusicEnabled = true;

    private const string MUSIC_ENABLED_KEY = "MusicEnabled";

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;

        // Load saved preference
        isMusicEnabled = PlayerPrefs.GetInt(MUSIC_ENABLED_KEY, 1) == 1;

        Debug.Log($"[MusicManager] Music enabled: {isMusicEnabled}");
    }

    void Start()
    {
        // Start playing music if enabled
        if (isMusicEnabled && backgroundMusic != null)
        {
            PlayMusic();
        }
    }

    /// <summary>
    /// Toggle music on/off
    /// </summary>
    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;

        if (isMusicEnabled)
        {
            PlayMusic();
        }
        else
        {
            StopMusic();
        }

        // Save preference
        PlayerPrefs.SetInt(MUSIC_ENABLED_KEY, isMusicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[MusicManager] Music toggled: {isMusicEnabled}");
    }

    /// <summary>
    /// Set music on/off
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        if (isMusicEnabled == enabled) return;

        isMusicEnabled = enabled;

        if (isMusicEnabled)
        {
            PlayMusic();
        }
        else
        {
            StopMusic();
        }

        // Save preference
        PlayerPrefs.SetInt(MUSIC_ENABLED_KEY, isMusicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"[MusicManager] Music set to: {isMusicEnabled}");
    }

    /// <summary>
    /// Check if music is currently enabled
    /// </summary>
    public bool IsMusicEnabled()
    {
        return isMusicEnabled;
    }

    /// <summary>
    /// Set the background music clip
    /// </summary>
    public void SetMusicClip(AudioClip clip)
    {
        bool wasPlaying = audioSource.isPlaying;

        audioSource.Stop();
        backgroundMusic = clip;
        audioSource.clip = clip;

        if (wasPlaying && isMusicEnabled)
        {
            audioSource.Play();
        }
    }

    /// <summary>
    /// Set the music volume (0-1)
    /// </summary>
    public void SetVolume(float vol)
    {
        volume = Mathf.Clamp01(vol);
        audioSource.volume = volume;
    }

    private void PlayMusic()
    {
        if (backgroundMusic == null)
        {
            Debug.LogWarning("[MusicManager] No background music clip assigned!");
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.clip = backgroundMusic;
            audioSource.Play();
            Debug.Log("[MusicManager] Music started");
        }
    }

    private void StopMusic()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[MusicManager] Music stopped");
        }
    }
}
