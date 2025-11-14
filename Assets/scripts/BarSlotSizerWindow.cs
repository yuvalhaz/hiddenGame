#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

public class BarSlotSizerWindow : EditorWindow
{
    [Header("Criteria")]
    string slotNameContains = "BarSlot";   // מחפש לפי שם ברירת מחדל
    bool includeInactive = true;

    [Header("Preferred Size")]
    float preferredWidth  = 160f;
    float preferredHeight = 160f;
    bool addIfMissing = true;

    [Header("Optional helpers")]
    bool ensureIconFitsInParent = true;   // מוסיף AspectRatioFitter ל-Icon אם קיים
    string iconChildName = "Icon";

    [MenuItem("Tools/UI/Bar Slot Sizer")]
    public static void Open()
    {
        GetWindow<BarSlotSizerWindow>("Bar Slot Sizer");
    }

    void OnGUI()
    {
        GUILayout.Label("Find Targets", EditorStyles.boldLabel);
        slotNameContains = EditorGUILayout.TextField(new GUIContent("Name Contains"), slotNameContains);
        includeInactive = EditorGUILayout.Toggle(new GUIContent("Include Inactive"), includeInactive);

        EditorGUILayout.Space();
        GUILayout.Label("Preferred Size", EditorStyles.boldLabel);
        preferredWidth  = EditorGUILayout.FloatField(new GUIContent("Preferred Width"),  preferredWidth);
        preferredHeight = EditorGUILayout.FloatField(new GUIContent("Preferred Height"), preferredHeight);
        addIfMissing    = EditorGUILayout.Toggle(new GUIContent("Add LayoutElement if missing"), addIfMissing);

        EditorGUILayout.Space();
        GUILayout.Label("Optional", EditorStyles.boldLabel);
        ensureIconFitsInParent = EditorGUILayout.Toggle(new GUIContent("Ensure Icon fits in parent (AspectRatioFitter)"), ensureIconFitsInParent);
        iconChildName          = EditorGUILayout.TextField(new GUIContent("Icon Child Name"), iconChildName);

        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply to Selected"))
        {
            ApplyToSelection();
        }
        if (GUILayout.Button("Apply to Scene"))
        {
            ApplyToScene();
        }
        if (GUILayout.Button("Apply to Prefabs in Folder..."))
        {
            ApplyToPrefabsInFolder();
        }
        EditorGUILayout.EndHorizontal();
    }

    void ApplyToSelection()
    {
        var changed = new List<GameObject>();
        foreach (var obj in Selection.gameObjects)
        {
            changed.AddRange(ApplyInHierarchy(obj.transform));
        }
        if (changed.Count > 0)
        {
            EditorUtility.SetDirty(this);
            Debug.Log($"[BarSlotSizer] Updated {changed.Count} object(s) in Selection.");
        }
        else
        {
            Debug.Log("[BarSlotSizer] No matching objects found in Selection.");
        }
    }

    void ApplyToScene()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var changed = new List<GameObject>();
        foreach (var root in roots)
        {
            changed.AddRange(ApplyInHierarchy(root.transform));
        }
        if (changed.Count > 0)
        {
            Debug.Log($"[BarSlotSizer] Updated {changed.Count} object(s) in Scene.");
        }
        else
        {
            Debug.Log("[BarSlotSizer] No matching objects found in Scene.");
        }
    }

    void ApplyToPrefabsInFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Pick a folder with prefabs", "Assets", "");
        if (string.IsNullOrEmpty(folder)) return;

        // המרה לנתיב יחסי ל-Assets
        var projectPath = Application.dataPath; // .../Project/Assets
        if (!folder.StartsWith(projectPath))
        {
            EditorUtility.DisplayDialog("Folder must be under Assets", "בחר תקייה מתוך Assets בלבד.", "OK");
            return;
        }
        string relativeFolder = "Assets" + folder.Substring(projectPath.Length);

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { relativeFolder });
        int changedCount = 0;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab) continue;

            bool modified = false;
            // פותח את הפריפאב לעריכה זמנית


            var root = PrefabUtility.LoadPrefabContents(path);
            var updated = ApplyInHierarchy(root.transform);
            if (updated.Count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                modified = true;
                changedCount += updated.Count;
            }
            PrefabUtility.UnloadPrefabContents(root);

            if (modified)
                Debug.Log($"[BarSlotSizer] Updated {updated.Count} object(s) in prefab: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BarSlotSizer] Done. Updated total {changedCount} object(s) in prefabs under: {relativeFolder}");
    }

    List<GameObject> ApplyInHierarchy(Transform root)
    {
        var changed = new List<GameObject>();
        var stack = new Stack<Transform>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t == null) continue;

            if (includeInactive || (t.gameObject.activeInHierarchy))
            {
                if (IsSlotCandidate(t.gameObject))
                {
                    if (ApplyToSlot(t.gameObject))
                        changed.Add(t.gameObject);
                }
                for (int i = 0; i < t.childCount; i++)
                    stack.Push(t.GetChild(i));
            }
        }
        return changed;
    }

    bool IsSlotCandidate(GameObject go)
    {
        if (string.IsNullOrEmpty(slotNameContains)) return false;
        return go.name.IndexOf(slotNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0
               && go.GetComponent<RectTransform>() != null;
    }

    bool ApplyToSlot(GameObject slot)
    {
        bool changed = false;

        var le = slot.GetComponent<LayoutElement>();
        if (le == null && addIfMissing)
        {
            le = Undo.AddComponent<LayoutElement>(slot);
            changed = true;
        }

        if (le != null)
        {
            Undo.RecordObject(le, "Set Preferred Size");
            le.preferredWidth  = preferredWidth;
            le.preferredHeight = preferredHeight;
            changed = true;
            EditorUtility.SetDirty(le);
        }

        if (ensureIconFitsInParent && !string.IsNullOrEmpty(iconChildName))
        {
            var icon = slot.transform.Find(iconChildName);
            if (icon != null)
            {
                var arf = icon.GetComponent<AspectRatioFitter>();
                if (arf == null)
                {
                    arf = Undo.AddComponent<AspectRatioFitter>(icon.gameObject);
                    changed = true;
                }
                Undo.RecordObject(arf, "Set AspectRatioFitter");
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                EditorUtility.SetDirty(arf);

                var iconRT = icon as RectTransform;
                if (iconRT != null)
                {
                    Undo.RecordObject(iconRT, "Set Icon Anchors");
                    iconRT.anchorMin = Vector2.zero;
                    iconRT.anchorMax = Vector2.one;
                    iconRT.offsetMin = Vector2.zero;
                    iconRT.offsetMax = Vector2.zero;
                    EditorUtility.SetDirty(iconRT);
                    changed = true;
                }
            }
        }

        return changed;
    }
}
#endif
