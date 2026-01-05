using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Centralized cache for DropSpots to avoid repeated FindObjectsOfType calls.
/// Provides fast lookup by spotId.
/// Auto-clears when scenes change.
/// </summary>
public static class DropSpotCache
{
    private static Dictionary<string, DropSpot> cache;
    private static bool isInitialized = false;

    // Auto-subscribe to scene changes
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Clear();
    }

    /// <summary>
    /// Get a DropSpot by its spotId. Refreshes cache if needed.
    /// </summary>
    public static DropSpot Get(string spotId)
    {
        if (!isInitialized || cache == null || cache.Count == 0)
        {
            Refresh();
        }

        if (string.IsNullOrEmpty(spotId))
        {
            Debug.LogWarning("[DropSpotCache] Attempted to get DropSpot with null/empty spotId");
            return null;
        }

        cache.TryGetValue(spotId, out DropSpot spot);
        return spot;
    }

    /// <summary>
    /// Refresh the cache by finding all DropSpots in the scene.
    /// Call this when DropSpots are added/removed dynamically.
    /// </summary>
    public static void Refresh()
    {
        if (cache == null)
        {
            cache = new Dictionary<string, DropSpot>();
        }
        else
        {
            cache.Clear();
        }

        // Find all DropSpots including inactive ones
        var allDropSpots = Object.FindObjectsOfType<DropSpot>(true);

        foreach (var spot in allDropSpots)
        {
            if (string.IsNullOrEmpty(spot.spotId))
            {
                Debug.LogWarning($"[DropSpotCache] DropSpot on '{spot.gameObject.name}' has empty spotId!");
                continue;
            }

            if (cache.ContainsKey(spot.spotId))
            {
                Debug.LogWarning($"[DropSpotCache] Duplicate spotId found: '{spot.spotId}'");
                continue;
            }

            cache[spot.spotId] = spot;
        }

        isInitialized = true;

        #if UNITY_EDITOR
        Debug.Log($"[DropSpotCache] Cached {cache.Count} DropSpots");
        #endif
    }

    /// <summary>
    /// Clear the cache. Useful for scene transitions.
    /// </summary>
    public static void Clear()
    {
        cache?.Clear();
        isInitialized = false;
    }

    /// <summary>
    /// Get the number of cached spots.
    /// </summary>
    public static int Count => cache?.Count ?? 0;

    /// <summary>
    /// Check if cache contains a specific spotId.
    /// </summary>
    public static bool Contains(string spotId)
    {
        return cache != null && cache.ContainsKey(spotId);
    }

    /// <summary>
    /// Get all cached DropSpots as an array.
    /// </summary>
    public static DropSpot[] GetAll()
    {
        if (!isInitialized || cache == null || cache.Count == 0)
        {
            Refresh();
        }

        if (cache == null || cache.Count == 0)
        {
            return new DropSpot[0];
        }

        DropSpot[] result = new DropSpot[cache.Count];
        cache.Values.CopyTo(result, 0);
        return result;
    }
}