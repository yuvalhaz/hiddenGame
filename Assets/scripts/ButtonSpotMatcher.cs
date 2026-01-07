using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// כלי עזר לבדיקת התאמה בין DraggableButtons ל-DropSpots
/// </summary>
public class ButtonSpotMatcher : MonoBehaviour
{
    [Header("🔍 בדיקה אוטומטית")]
    [SerializeField] private ScrollableButtonBar buttonBar;
    [SerializeField] private GameObject dropSpotsContainer;
    
    [ContextMenu("🔍 בדוק התאמות")]
    public void CheckMatches()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("🔍 בודק התאמות בין כפתורים ל-DropSpots");
        Debug.Log("════════════════════════════════════════\n");
        
        // מצא את כל הכפתורים
        DraggableButton[] buttons = buttonBar != null 
            ? buttonBar.GetComponentsInChildren<DraggableButton>(true)
            : FindObjectsOfType<DraggableButton>(true);
            
        // מצא את כל ה-DropSpots
        DropSpot[] spots = dropSpotsContainer != null
            ? dropSpotsContainer.GetComponentsInChildren<DropSpot>(true)
            : FindObjectsOfType<DropSpot>(true);
        
        Debug.Log($"📊 נמצאו {buttons.Length} כפתורים");
        Debug.Log($"📊 נמצאו {spots.Length} DropSpots\n");
        
        // צור רשימות של IDs
        List<string> buttonIDs = new List<string>();
        List<string> spotIDs = new List<string>();
        
        Debug.Log("📋 רשימת כפתורים:");
        foreach (var btn in buttons)
        {
            string id = btn.GetButtonID();
            buttonIDs.Add(id);
            bool placed = btn.HasBeenPlaced();
            Debug.Log($"   🔹 {id} | הוצב: {placed} | GameObject: {btn.gameObject.name}");
        }
        
        Debug.Log("\n📋 רשימת DropSpots:");
        foreach (var spot in spots)
        {
            string id = spot.spotId;
            spotIDs.Add(id);
            bool settled = spot.IsSettled;
            Debug.Log($"   🔹 {id} | מאוכלס: {settled} | GameObject: {spot.gameObject.name}");
        }
        
        // בדוק אי-התאמות
        Debug.Log("\n🔍 בדיקת אי-התאמות:");
        
        // כפתורים ללא DropSpot
        var buttonsWithoutSpot = buttonIDs.Where(bid => !spotIDs.Contains(bid)).ToList();
        if (buttonsWithoutSpot.Count > 0)
        {
            Debug.LogError($"❌ כפתורים ללא DropSpot מתאים ({buttonsWithoutSpot.Count}):");
            foreach (var id in buttonsWithoutSpot)
                Debug.LogError($"   ❌ {id}");
        }
        else
        {
            Debug.Log("✅ כל הכפתורים יש להם DropSpot מתאים!");
        }
        
        // DropSpots ללא כפתור
        var spotsWithoutButton = spotIDs.Where(sid => !buttonIDs.Contains(sid)).ToList();
        if (spotsWithoutButton.Count > 0)
        {
            Debug.LogWarning($"⚠️ DropSpots ללא כפתור מתאים ({spotsWithoutButton.Count}):");
            foreach (var id in spotsWithoutButton)
                Debug.LogWarning($"   ⚠️ {id}");
        }
        else
        {
            Debug.Log("✅ כל ה-DropSpots יש להם כפתור מתאים!");
        }
        
        Debug.Log("\n════════════════════════════════════════");
        
        if (buttonsWithoutSpot.Count == 0 && spotsWithoutButton.Count == 0)
        {
            Debug.Log("🎉 הכל מתאים בצורה מושלמת!");
        }
        else
        {
            Debug.LogWarning("⚠️ יש אי-התאמות שצריך לתקן!");
        }
        
        Debug.Log("════════════════════════════════════════\n");
    }
    
    [ContextMenu("🔧 תקן שמות אוטומטית (DropSpots → Buttons)")]
    public void AutoFixSpotNames()
    {
        Debug.Log("🔧 מתקן שמות DropSpots לפי הכפתורים...\n");
        
        DraggableButton[] buttons = buttonBar != null 
            ? buttonBar.GetComponentsInChildren<DraggableButton>(true)
            : FindObjectsOfType<DraggableButton>(true);
            
        DropSpot[] spots = dropSpotsContainer != null
            ? dropSpotsContainer.GetComponentsInChildren<DropSpot>(true)
            : FindObjectsOfType<DropSpot>(true);
        
        if (buttons.Length != spots.Length)
        {
            Debug.LogError($"❌ כמות לא תואמת! כפתורים: {buttons.Length}, DropSpots: {spots.Length}");
            return;
        }
        
        for (int i = 0; i < buttons.Length && i < spots.Length; i++)
        {
            string buttonID = buttons[i].GetButtonID();
            string oldSpotID = spots[i].spotId;
            
            if (buttonID != oldSpotID)
            {
                Debug.Log($"🔄 משנה {oldSpotID} → {buttonID}");
                
                // ⚠️ זה לא יעבוד בזמן ריצה! רק ב-Editor
                #if UNITY_EDITOR
                UnityEditor.Undo.RecordObject(spots[i], "Fix Spot ID");
                spots[i].spotId = buttonID;
                UnityEditor.EditorUtility.SetDirty(spots[i]);
                #endif
            }
        }
        
        Debug.Log("✅ סיום תיקון שמות!\n");
        
        #if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        #endif
    }
    
    [ContextMenu("📝 ייצא רשימת התאמות")]
    public void ExportMatches()
    {
        Debug.Log("════════════════════════════════════════");
        Debug.Log("📝 רשימת התאמות:");
        Debug.Log("════════════════════════════════════════\n");
        
        DraggableButton[] buttons = buttonBar != null 
            ? buttonBar.GetComponentsInChildren<DraggableButton>(true)
            : FindObjectsOfType<DraggableButton>(true);
            
        DropSpot[] spots = dropSpotsContainer != null
            ? dropSpotsContainer.GetComponentsInChildren<DropSpot>(true)
            : FindObjectsOfType<DropSpot>(true);
        
        Debug.Log("Button ID → DropSpot ID");
        Debug.Log("─────────────────────────");
        
        for (int i = 0; i < Mathf.Max(buttons.Length, spots.Length); i++)
        {
            string buttonID = i < buttons.Length ? buttons[i].GetButtonID() : "[חסר]";
            string spotID = i < spots.Length ? spots[i].spotId : "[חסר]";
            string match = buttonID == spotID ? "✅" : "❌";
            
            Debug.Log($"{i:D2}. {buttonID,-15} ←→ {spotID,-15} {match}");
        }
        
        Debug.Log("\n════════════════════════════════════════\n");
    }
}