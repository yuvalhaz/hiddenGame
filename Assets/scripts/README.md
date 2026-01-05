# Hidden Game - Scripts Documentation

This is the centralized location for all game scripts. All scripts are organized by functionality for easy navigation and maintenance.

## Directory Structure

```
Assets/scripts/
├── Ads/              # Advertisement & monetization
├── Managers/         # Core game management systems
├── UI/               # User interface components
├── Reveal/           # Image reveal mechanics
└── README.md         # This file
```

## Script Categories

### 📱 Ads/ - Advertisement Management
Core advertisement functionality for the game.

- **AdInit.cs** - Initializes and preloads ads at game start
- **RewardedAdsManager.cs** - Manages rewarded video advertisements

### 🎮 Managers/ - Core Game Systems
Central management systems that control game flow and state.

- **GameProgressManager.cs** - Handles save/load, tracks placed items, manages game state persistence
- **LevelManager.cs** - Controls level progression, validates item placement, manages level completion
- **DropSpotBatchManager.cs** - Batch operations for drop spots

### 🎨 UI/ - User Interface
All UI-related components and interactions.

- **DraggableButton.cs** - Draggable items that players can move around
- **DropSpot.cs** - Target locations where items can be dropped
- **ScrollableButtonBar.cs** - Scrollable container for draggable buttons
- **HintButton.cs** - Button to trigger hints
- **HintDialog.cs** - Dialog system for displaying hints
- **UIConfetti.cs** - Confetti visual effects for celebrations

### 🖼️ Reveal/ - Image Reveal System
Mechanics for revealing hidden images as players progress.

- **ImageRevealController.cs** - Controls the reveal animation and state
- **ItemRevealConfig.cs** - Configuration data for reveal behavior

## Quick Reference

### Finding Scripts by Function

**Need to modify save/load behavior?**
→ `Managers/GameProgressManager.cs`

**Need to change level progression?**
→ `Managers/LevelManager.cs`

**Need to adjust drag & drop mechanics?**
→ `UI/DraggableButton.cs` and `UI/DropSpot.cs`

**Need to modify ad behavior?**
→ `Ads/RewardedAdsManager.cs` and `Ads/AdInit.cs`

**Need to change reveal animations?**
→ `Reveal/ImageRevealController.cs`

## Script Dependencies

```
GameProgressManager (Singleton)
    ↓
LevelManager
    ├── uses GameProgressManager
    └── uses RewardedAdsManager

DraggableButton
    ├── uses DropSpot
    └── notifies GameProgressManager

ImageRevealController
    └── uses ItemRevealConfig
```

## Development Guidelines

1. **Always check this centralized location first** when looking for scripts
2. **Keep scripts in their appropriate category folders**
3. **Update this README** when adding new scripts or categories
4. **Follow naming conventions**: PascalCase for all script names

## Adding New Scripts

When adding new scripts:

1. Determine which category it belongs to
2. Place it in the appropriate folder
3. Update this README with the script name and description
4. Commit changes with a clear message

---

**Last Updated:** 2026-01-05
**Total Scripts:** 13
**Game Type:** Hidden Object / Puzzle Game
