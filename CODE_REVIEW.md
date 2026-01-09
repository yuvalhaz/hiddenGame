# Hidden Game - Code Review

**Review Date:** January 9, 2026
**Branch:** claude/review-game-scripts-N3Fz2
**Scripts Reviewed:** 13 local scripts (13/36 from mainV7)

## Executive Summary

This is a Unity-based hidden object puzzle game with drag-and-drop mechanics, level progression, batch-based item revealing, and ad monetization. The codebase demonstrates good architectural patterns but has areas for improvement in consistency, completeness, and maintainability.

## Repository Comparison

### Local Scripts (13 files)
- AdInit.cs
- DraggableButton.cs
- DropSpot.cs
- DropSpotBatchManager.cs
- GameProgressManager.cs
- HintButton.cs
- HintDialog.cs
- ImageRevealController.cs
- ItemRevealConfig.cs
- LevelManager.cs
- RewardedAdsManager.cs
- ScrollableButtonBar.cs
- UIConfetti.cs

### Missing Scripts (23 files from mainV7)
The following scripts exist on GitHub mainV7 but are missing locally:
- LevelData.cs
- LevelSelectionUI.cs
- LevelCompleteController.cs
- BarSlotSizerWindow.cs
- DebugMenuUI.cs
- BatchProgressUI.cs
- BatchMessageController.cs
- EndingDialogController.cs
- TutorialSlideManager.cs
- DragDropValidator.cs
- DragAnimator.cs
- DragVisualManager.cs
- ButtonSpotMatcher.cs
- DropSpotCache.cs
- BatchCompletionCelebration.cs
- BatchAdController.cs
- AdMobConfig.cs
- InterstitialAdsManager.cs
- VisualHintSystem.cs
- SparkleBurstEffect.cs
- LoadingManager.cs
- GameDebugTools.cs

## Code Quality Analysis

### Strengths

#### 1. Architecture
- **Singleton Pattern**: Properly implemented in `LevelManager`, `GameProgressManager`, and `RewardedAdsManager`
- **Event-Driven Design**: Good use of C# events (`OnItemPlaced`, `OnLevelCompleted`, etc.)
- **Separation of Concerns**: Clear division between game logic, UI, and data management

#### 2. Unity Best Practices
- Proper use of `DontDestroyOnLoad` for persistent managers
- `SerializeField` with private fields for encapsulation
- Context menus for debugging (`[ContextMenu]`)
- ScriptableObject for configuration (`ItemRevealConfig`)

#### 3. Save/Load System
- JSON-based serialization in `GameProgressManager`
- Auto-save functionality with configurable intervals
- Proper error handling in save/load operations

#### 4. Debug Support
- Comprehensive debug logging throughout
- Debug mode toggles in inspector
- Test methods via Context Menus
- Extensive validation in `OnValidate()`

### Issues and Concerns

#### 1. Critical Issues

**Missing Ad Implementation (Priority: HIGH)**
```csharp
// RewardedAdsManager.cs lines 29-31, 39-41, 66-76
// TODO comments indicate placeholder implementation
#else
    // TODO: טען מודעת Rewarded אמיתית
    return true;
#endif
```
**Impact**: Game has ad integration points but no actual ad SDK implementation.

**Fragile Reflection Usage (Priority: MEDIUM)**
```csharp
// GameProgressManager.cs:484-506
var barScript = bar.GetType();
var buttonStatesField = barScript.GetField("buttonStates",
    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
```
**Issue**: Using reflection to access private fields is fragile and breaks encapsulation.
**Recommendation**: Create public methods in `ScrollableButtonBar` for these operations.

**Unchecked FindObjectOfType Calls (Priority: LOW)**
```csharp
// LevelManager.cs:59-60
if (!progressManager) progressManager = FindObjectOfType<GameProgressManager>();
if (!adsManager) adsManager = FindObjectOfType<RewardedAdsManager>();
```
**Issue**: `FindObjectOfType` is expensive and can return null.
**Recommendation**: Add null checks and log errors if required managers are missing.

#### 2. Code Quality Issues

**Mixed Language Comments**
```csharp
// Hebrew comments throughout the codebase
// Example from DraggableButton.cs:60
Debug.Log($"[DraggableButton] OnDestroy - מנקה activeDragRT");
```
**Impact**: Makes code harder to maintain for international developers.
**Recommendation**: Standardize on English for code comments.

**No Namespace Usage**
All scripts are in the global namespace, increasing risk of naming conflicts.
```csharp
// All files lack namespace declarations
public class LevelManager : MonoBehaviour
```
**Recommendation**: Add namespace like `namespace HiddenGame.Core`

**Magic Numbers**
```csharp
// DraggableButton.cs
private float dropDistanceThreshold = 150f;
Vector2 targetSize = targetSize * 0.3f;
return new Vector2(350f, 350f);
```
**Recommendation**: Extract to named constants or serialized fields.

**Inconsistent Error Handling**
```csharp
// Some methods use Debug.LogError
Debug.LogError($"[DraggableButton] ❌ DropSpot has no RectTransform!");
// Others use Debug.LogWarning for similar severity
Debug.LogWarning($"[LevelManager] Item {itemId} was placed but doesn't belong to current level!");
```
**Recommendation**: Establish consistent severity guidelines.

#### 3. Potential Bugs

**Cache Initialization Race Condition**
```csharp
// DraggableButton.cs:411-419
private Sprite GetRealPhotoFromDropSpot()
{
    if (dropSpotCache == null || dropSpotCache.Count == 0)
    {
        RefreshDropSpotCache();
    }
    // Cache may still be empty if no DropSpots found
}
```
**Issue**: If `RefreshDropSpotCache()` finds no spots, the cache remains empty but the code continues.

**Coroutine Not Stopped on Destroy**
```csharp
// DropSpotBatchManager.cs:133
private Coroutine hideMessageCoroutine = null;
// No cleanup in OnDestroy if coroutine is running
```
**Impact**: Memory leak potential.

**Button State Desync Risk**
```csharp
// ScrollableButtonBar.cs:125-133
for (int i = 0; i < buttons.Count; i++)
{
    if (buttons[i] == null)
    {
        buttonStates[i] = false;
    }
}
```
**Issue**: If buttons and buttonStates lists get out of sync, IndexOutOfRange exception.

#### 4. Performance Concerns

**FindObjectOfType in Hot Path**
```csharp
// DraggableButton.cs:248-258, 333-344
Canvas[] canvases = FindObjectsOfType<Canvas>();
```
**Impact**: Called frequently during drag operations.
**Recommendation**: Cache canvas reference.

**Static Cache Never Cleared**
```csharp
// DraggableButton.cs:34
private static Dictionary<string, DropSpot> dropSpotCache;
```
**Issue**: Static cache persists across scene loads, may reference destroyed objects.

**Reflection in Update Loop**
```csharp
// GameProgressManager.cs:475-507 (called from coroutine)
var method = barScript.GetMethod("RecalculateAllPositions", ...)
```
**Impact**: Slow reflection calls in performance-sensitive code.

#### 5. Maintainability Issues

**Long Methods**
- `DraggableButton.OnDrag()` - 40+ lines
- `DropSpotBatchManager.AnimateMessage()` - 70+ lines with switch statement
- `GameProgressManager.ApplyProgressToScene()` - 65+ lines

**Complex Coroutine Chains**
```csharp
// DropSpotBatchManager.cs:398-474
private IEnumerator ShowAdAndContinue()
{
    // Multiple nested waits and callbacks
    // Complex state management
}
```

**God Class Warning**
`DropSpotBatchManager` has 936 lines with many responsibilities:
- Batch management
- UI updates
- Animation
- Audio
- Ad integration
- Message display

**Recommendation**: Extract separate classes for:
- `BatchAnimationController`
- `BatchMessageDisplay`
- `BatchProgressTracker`

## Security Considerations

### Data Persistence
```csharp
// GameProgressManager.cs:47
private const string SAVE_KEY = "GameProgress";
```
**Status**: Uses PlayerPrefs (not encrypted)
**Risk**: Low - casual game, no sensitive data
**Recommendation**: Document that PlayerPrefs can be edited by users

### Input Validation
**Status**: Minimal validation on item IDs and save data
**Risk**: Low - single-player game
**Note**: Add validation if multiplayer features are planned

## Testing Gaps

### No Unit Tests
The codebase lacks unit tests. Critical areas needing coverage:
- `LevelManager.IsCurrentLevelComplete()`
- `GameProgressManager` save/load logic
- `DropSpotBatchManager` batch calculation
- `ScrollableButtonBar` position calculations

### Debugging Features
Good debug support via Context Menus, but could benefit from:
- In-game debug console
- State visualization overlay
- Performance profiler integration

## Recommendations

### Immediate Actions (High Priority)

1. **Complete Ad Integration**
   - Implement Google Mobile Ads SDK integration
   - Replace placeholder code in `RewardedAdsManager`
   - Add `InterstitialAdsManager` from mainV7

2. **Fix Reflection Usage**
   ```csharp
   // In ScrollableButtonBar, add public methods:
   public void MarkButtonInactive(int index) {
       if (index >= 0 && index < buttonStates.Count)
           buttonStates[index] = false;
   }
   public void RecalculatePositions() {
       RecalculateAllPositions();
   }
   ```

3. **Add Missing Scripts**
   - Pull 23 missing scripts from mainV7
   - Document why scripts are missing
   - Update dependencies

### Short-term Improvements (Medium Priority)

4. **Standardize Logging**
   ```csharp
   // Create a Logger utility class
   public static class GameLogger {
       public static void LogError(string context, string message) { }
       public static void LogWarning(string context, string message) { }
       public static void Log(string context, string message, bool debugOnly = false) { }
   }
   ```

5. **Add Namespaces**
   ```csharp
   namespace HiddenGame.Core { }
   namespace HiddenGame.UI { }
   namespace HiddenGame.Ads { }
   ```

6. **Cache Optimization**
   ```csharp
   // In DraggableButton, add scene change cleanup:
   void OnEnable() {
       SceneManager.sceneLoaded += OnSceneLoaded;
   }
   void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
       dropSpotCache = null; // Force refresh
   }
   ```

7. **Extract Constants**
   ```csharp
   // Create GameConstants.cs
   public static class GameConstants {
       public const float DROP_DISTANCE_THRESHOLD = 150f;
       public const float DEFAULT_PHOTO_SIZE = 350f;
       public const int ITEMS_PER_LEVEL = 7;
   }
   ```

### Long-term Enhancements (Low Priority)

8. **Add Unit Tests**
   - Use Unity Test Framework
   - Start with critical game logic
   - Aim for 60%+ code coverage

9. **Refactor God Classes**
   - Split `DropSpotBatchManager` into smaller classes
   - Apply Single Responsibility Principle
   - Improve testability

10. **Documentation**
    - Add XML documentation comments to public APIs
    - Create architecture diagram
    - Document save file format
    - Add gameplay flow diagrams

11. **Performance Profiling**
    - Profile drag-and-drop operations
    - Optimize FindObjectOfType calls
    - Consider object pooling for UI elements

## Conclusion

### Overall Assessment: **GOOD** ⭐⭐⭐⭐☆

The codebase is functional and demonstrates solid Unity development practices. The architecture is sound with good separation of concerns and proper use of design patterns. However, there are areas needing attention:

**Strengths:**
- Clean architecture with proper separation
- Good use of Unity patterns
- Comprehensive debug support
- Solid save/load system

**Weaknesses:**
- Incomplete ad integration (critical)
- Missing 23 scripts from mainV7
- Fragile reflection usage
- Mixed language comments
- Lack of unit tests

### Risk Assessment
- **Critical Risks**: Ad integration incomplete
- **Medium Risks**: Reflection usage, missing scripts
- **Low Risks**: Performance optimizations, code style

### Next Steps
1. Complete ad SDK integration
2. Sync missing scripts from mainV7
3. Address reflection usage in GameProgressManager
4. Standardize code comments to English
5. Add namespaces to all scripts

---

**Reviewer Notes:**
This game shows promise with a well-structured codebase. The development team clearly understands Unity best practices. Addressing the missing ad implementation and syncing with mainV7 should be top priorities before production release.
