# Hidden Game - Scripts Documentation 🎮

**Branch:** mainv6
**Last Updated:** 2026-01-05
**Total Scripts:** 35

This is the centralized location for ALL game scripts. Every script is organized by functionality for maximum efficiency and easy navigation.

---

## 📁 Directory Structure

```
Assets/scripts/
├── Ads/              # Advertisement & Monetization (5 scripts)
├── Managers/         # Core Game Systems (8 scripts)
├── UI/               # User Interface Components (13 scripts)
├── Reveal/           # Image Reveal Mechanics (2 scripts)
├── DragDrop/         # Drag & Drop System (3 scripts)
├── Utilities/        # Helper Tools & Debug (2 scripts)
├── Editor/           # Unity Editor Tools (1 script)
├── Effects/          # Visual Effects (1 script)
└── README.md         # This file
```

---

## 📱 Ads/ - Advertisement & Monetization
Complete advertisement system with AdMob integration.

| Script | Description |
|--------|-------------|
| **AdInit.cs** | Initializes AdMob SDK and preloads ads |
| **AdMobConfig.cs** | Configuration for AdMob (test/prod mode, ad unit IDs) |
| **BatchAdController.cs** | Controls ads shown between batches/levels |
| **InterstitialAdsManager.cs** | Manages interstitial (fullscreen) ads |
| **RewardedAdsManager.cs** | Manages rewarded video ads for hints/rewards |

---

## 🎮 Managers/ - Core Game Systems
Central management systems controlling game flow, progress, and levels.

| Script | Description |
|--------|-------------|
| **GameProgressManager.cs** | Save/load system, tracks placed items, manages persistence |
| **LevelManager.cs** | Level progression, completion tracking, scene management |
| **DropSpotBatchManager.cs** | Batch operations for managing groups of drop spots |
| **DropSpotCache.cs** | Caching system for optimizing drop spot lookups |
| **LoadingManager.cs** | Handles loading screens between scenes |
| **LevelData.cs** | Data structure for level configuration |
| **LevelCompleteController.cs** | Handles level completion logic and celebrations |
| **EndingDialogController.cs** | Controls ending dialog bubbles and level exit flow |

---

## 🎨 UI/ - User Interface Components
All UI-related components, dialogs, and interactive elements.

| Script | Description |
|--------|-------------|
| **DraggableButton.cs** | Main draggable item that players interact with |
| **DropSpot.cs** | Target locations where items can be dropped |
| **ScrollableButtonBar.cs** | Scrollable container for draggable buttons at bottom |
| **HintButton.cs** | Button to trigger hint system |
| **HintDialog.cs** | Dialog for displaying hints to players |
| **UIConfetti.cs** | Confetti celebration effects for achievements |
| **BatchProgressUI.cs** | Progress bar/UI for batch completion |
| **BatchMessageController.cs** | Messages shown during batch transitions |
| **LevelSelectionUI.cs** | Level selection screen with unlock logic |
| **DebugMenuUI.cs** | In-game debug menu for testing |
| **TutorialSlideManager.cs** | Tutorial slides system for onboarding |
| **BatchCompletionCelebration.cs** | Celebration effects when batch is complete |
| **VisualHintSystem.cs** | Visual hint overlay system |

---

## 🖼️ Reveal/ - Image Reveal System
Mechanics for revealing hidden images as players progress.

| Script | Description |
|--------|-------------|
| **ImageRevealController.cs** | Controls reveal animation, fade, and progression |
| **ItemRevealConfig.cs** | Configuration data for reveal behavior |

---

## 🖱️ DragDrop/ - Drag & Drop System
Complete drag-and-drop interaction system.

| Script | Description |
|--------|-------------|
| **DragAnimator.cs** | Animates dragging movement and transitions |
| **DragDropValidator.cs** | Validates if drop is allowed on target |
| **DragVisualManager.cs** | Manages visual feedback during drag operations |

---

## 🛠️ Utilities/ - Helper Tools & Debug
Utility scripts for development and debugging.

| Script | Description |
|--------|-------------|
| **ButtonSpotMatcher.cs** | Utility to match buttons with drop spots |
| **GameDebugTools.cs** | Comprehensive debug tools for testing |

---

## ⚙️ Editor/ - Unity Editor Tools
Tools that only run in Unity Editor for development.

| Script | Description |
|--------|-------------|
| **BarSlotSizerWindow.cs** | Editor window for configuring button bar slots |

---

## ✨ Effects/ - Visual Effects
Particle effects and visual enhancements.

| Script | Description |
|--------|-------------|
| **SparkleBurstEffect.cs** | Sparkle burst particle effect for celebrations |

---

## 🔍 Quick Reference Guide

### Common Tasks

**Modify save/load behavior?**
→ `Managers/GameProgressManager.cs`

**Change level progression?**
→ `Managers/LevelManager.cs`

**Adjust drag & drop mechanics?**
→ `DragDrop/DragAnimator.cs`, `DragDrop/DragDropValidator.cs`, `UI/DraggableButton.cs`

**Modify ads behavior?**
→ `Ads/RewardedAdsManager.cs`, `Ads/InterstitialAdsManager.cs`

**Change reveal animations?**
→ `Reveal/ImageRevealController.cs`

**Update UI elements?**
→ Check `UI/` folder for specific component

**Debug issues?**
→ `Utilities/GameDebugTools.cs`, `UI/DebugMenuUI.cs`

---

## 🔗 Script Dependencies

```
┌─────────────────────────────────────────┐
│         GameProgressManager             │
│          (Singleton - DontDestroyOnLoad)│
└────────────────┬────────────────────────┘
                 │
      ┌──────────┴──────────┐
      ▼                     ▼
┌─────────────┐      ┌──────────────┐
│LevelManager │      │DraggableButton│
│             │◄─────┤              │
└──────┬──────┘      └──────┬───────┘
       │                    │
       ▼                    ▼
┌─────────────┐      ┌──────────────┐
│RewardedAds  │      │  DropSpot    │
│Manager      │      │              │
└─────────────┘      └──────────────┘
```

---

## 📊 Script Statistics

- **Total Scripts:** 35
- **Ads System:** 5 scripts
- **Core Managers:** 8 scripts
- **UI Components:** 13 scripts
- **Drag & Drop:** 3 scripts
- **Effects & Utils:** 6 scripts

---

## 💡 Development Guidelines

1. ✅ **Always check this centralized location first** when looking for scripts
2. ✅ **Keep scripts in their appropriate category folders**
3. ✅ **Update this README** when adding new scripts
4. ✅ **Follow Unity naming conventions**: PascalCase for all C# files
5. ✅ **Add XML documentation** to public methods
6. ✅ **Use [SerializeField]** instead of public fields

---

## ➕ Adding New Scripts

When adding new scripts:

1. **Determine category** - Which folder does it belong to?
2. **Place in appropriate folder** - Keep organization clean
3. **Update this README** - Add to the table in the correct section
4. **Commit with clear message** - Describe what the script does

Example commit message:
```
Add PowerUpManager to Managers/

- Handles power-up collection and activation
- Integrates with GameProgressManager
- Includes save/load for active power-ups
```

---

## 🎯 Game Architecture Overview

### Core Loop
1. **LevelManager** loads level scene
2. **GameProgressManager** restores saved progress
3. **DraggableButton** items appear in **ScrollableButtonBar**
4. Player drags to **DropSpot** targets
5. **ImageRevealController** reveals hidden image
6. **LevelCompleteController** triggers on completion
7. **RewardedAdsManager** shows ad (optional)
8. **EndingDialogController** provides exit options

### Save System
- Uses **PlayerPrefs** with JSON serialization
- Per-level save keys: `Level_X_Progress`
- Auto-save every 10 seconds (configurable)
- Manual save on item placement

### Ad System
- AdMob SDK integration
- Test mode for development
- Production mode for release
- Rewarded ads for hints/level skip
- Interstitial ads between levels

---

**Happy Coding! 🚀**

This structure ensures maximum efficiency when I need to find and modify scripts.
All scripts are exactly where they should be, categorized by functionality.
