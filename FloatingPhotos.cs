using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class FloatingPhotos : MonoBehaviour
{
    [Header("Photo Settings")]
    [Tooltip("Add your existing photo GameObjects here")]
    public GameObject[] photoObjects;
    
    [Header("Movement Settings")]
    [Tooltip("How fast photos drift around")]
    public float floatSpeed = 20f;
    
    [Tooltip("How fast photos rotate")]
    public float rotationSpeed = 30f;
    
    [Tooltip("Area where photos can float (width, height)")]
    public Vector2 boundsSize = new Vector2(1920f, 1080f);
    
    private List<PhotoData> photos = new List<PhotoData>();
    
    private class PhotoData
    {
        public RectTransform rectTransform;
        public Vector2 velocity;
        public float rotationVelocity;
    }
    
    void Start()
    {
        SetupPhotos();
    }
    
    void SetupPhotos()
    {
        if (photoObjects == null || photoObjects.Length == 0)
        {
            Debug.LogWarning("No photo GameObjects assigned!");
            return;
        }
        
        foreach (GameObject photoObj in photoObjects)
        {
            if (photoObj == null) continue;
            
            // Get RectTransform
            RectTransform rt = photoObj.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError($"{photoObj.name} doesn't have RectTransform!");
                continue;
            }
            
            // Create photo data with random velocity
            PhotoData photoData = new PhotoData
            {
                rectTransform = rt,
                velocity = new Vector2(
                    Random.Range(-floatSpeed, floatSpeed),
                    Random.Range(-floatSpeed, floatSpeed)
                ),
                rotationVelocity = Random.Range(-rotationSpeed, rotationSpeed)
            };
            
            photos.Add(photoData);
        }
        
        Debug.Log($"Setup {photos.Count} floating photos");
    }
    
    void Update()
    {
        foreach (PhotoData photo in photos)
        {
            // Float movement from current position
            photo.rectTransform.anchoredPosition += photo.velocity * Time.deltaTime;
            
            // Rotate
            photo.rectTransform.Rotate(0, 0, photo.rotationVelocity * Time.deltaTime);
            
            // Bounce off boundaries
            Vector2 pos = photo.rectTransform.anchoredPosition;
            
            if (Mathf.Abs(pos.x) > boundsSize.x / 2)
            {
                photo.velocity.x *= -1;
                pos.x = Mathf.Clamp(pos.x, -boundsSize.x / 2, boundsSize.x / 2);
            }
            
            if (Mathf.Abs(pos.y) > boundsSize.y / 2)
            {
                photo.velocity.y *= -1;
                pos.y = Mathf.Clamp(pos.y, -boundsSize.y / 2, boundsSize.y / 2);
            }
            
            photo.rectTransform.anchoredPosition = pos;
        }
    }
}