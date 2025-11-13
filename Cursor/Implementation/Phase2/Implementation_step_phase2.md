# Phase 2 – Creative Mode Implementation Specification

## 1. Purpose
- Enable a voice-driven creative drawing mode using the Phase 1 core painting systems.
- Remove competitive battle elements; focus on free-form painting, editing, and content persistence.

## 2. Scope
- Applies to `CreativeModeManager`, paint/eraser tooling, color selection, UI, persistence, and canvas extensions introduced in Phase 2.
- Reuses Phase 1 systems (`PaintCanvas`, `PaintSystem`, `PaintBattleGameManager`) while disabling combat-specific behavior.

## 3. Functional Requirements
- **Painting**: Map pitch/volume to screen coordinates and paint using the active color with adjustable intensity.
- **Tool Modes**: Support paint and eraser modes; allow switching via UI and expose state through events.
- **Undo**: Maintain canvas history and restore the previous state based on the configured save mode (operation-based or time-based).
- **Color Selection**: Provide multiple selection strategies (buttons, color picker, presets, optional voice-based).
- **Canvas Management**: Clear canvas, erase localized regions, reset to default, and export/import canvas state.
- **Saving & Sharing**: Persist the current canvas as PNG to disk and provide platform-specific sharing hooks.
- **UI Feedback**: Display current tool, active color, undo availability, and expose controls for all creative actions.

## 4. System Components
### 4.1 CreativeModeManager (`Assets/Scripts/Creative/CreativeModeManager.cs`)
- Manages creative mode lifecycle, tool selection, color application, and undo/redo history.
- Subscribes to voice volume events to detect silence and segment operations.
- Raises `OnToolModeChanged` / `OnColorChanged` events for UI updates.
- Depends on:
  - `PaintCanvas` (Phase 1) for paint data storage and manipulation.
  - `PaintBattleGameManager` for voice → position mapping (with combat logic disabled).
  - `CreativeModeSettings` ScriptableObject for configurable parameters.

### 4.2 CreativeModeSettings (`Assets/Script/Creative/CreativeModeSettings.cs`)
- Configurable paint/eraser intensities, available colors, history mode, silence thresholds, and optional analyzer references.
- Ensures non-programmers can balance tool behavior via Inspector.

### 4.3 PaintCanvas Extensions (`Assets/Script/GameLogic/PaintCanvas.cs`)
- New APIs: `EraseAt`, `GetState`, `RestoreState`, `GetTexture`.
- Supports canvas history snapshots (`CanvasState`) and texture export for saving.
- Emits events on paint/erase for other systems (e.g., color selectors, analytics).

### 4.4 ColorSelectionSystem (`Assets/Script/Creative/ColorSelectionSystem.cs`)
- Centralizes color selection strategy via `IColorSelector`.
- Selectors include:
  - `ButtonColorSelector`, `ColorPickerSelector`, `PresetPaletteSelector`, `VoiceColorSelector`.
- Controlled by `ColorSelectionSettings` ScriptableObject; publishes `OnColorSelected`.

### 4.5 CreativeModeUI (`Assets/Script/UI/CreativeModeUI.cs`)
- Presents controls for clear, undo, save, share, tool switching, and color selection.
- Generates preset color buttons from `CreativeModeSettings`.
- Toggles optional color picker panel; updates tool and color display using subscribed events.

### 4.6 CreativeModeSaveSystem (`Assets/Script/Creative/CreativeModeSaveSystem.cs`)
- Saves canvas to PNG using configurable directory and filename format (`SaveSettings`).
- Handles platform-specific share flows (Android/iOS/Desktop).
- Raises `OnImageSaved` / `OnShareCompleted` for UI confirmations.

## 5. Data Structures
- `CreativeToolMode` enum: `Paint`, `Eraser`.
- `CanvasState` class: serialized snapshot of `playerIdData` & `intensityData`.
- `CreativeModeSettings`, `ColorSelectionSettings`, `SaveSettings` ScriptableObjects for Inspector-driven configuration.
- `Stack<CanvasState>` in `CreativeModeManager` for undo history.

## 6. Workflow Summary
1. Voice analyzers emit pitch/volume → `PaintBattleGameManager` maps to screen position.
2. `CreativeModeManager.PaintAt` processes input based on active tool:
   - Paint: delegates to `PaintCanvas.PaintAt` with color/intensity adjustments.
   - Eraser: calls `PaintCanvas.EraseAt`.
3. Operation-based history saves a pre-operation snapshot when sound resumes and a post-operation snapshot after silence.
4. UI reacts to events, providing user feedback and invoking manager actions.
5. Saving/export functions request a `Texture2D` from `PaintCanvas`, encode to PNG, and persist/share per platform.

## 7. Non-Functional Requirements
- **Modularity**: All configurable parameters exposed through ScriptableObjects with `[Header]`, `[Tooltip]`, `[Range]`.
- **Extensibility**: Additional tools or selectors should be addable via interfaces (`CreativeToolMode`, `IColorSelector`).
- **Performance**: Limit history size (`maxHistorySize`), avoid redundant texture updates, and throttle auto-save interval.
- **Accessibility**: Provide clear UI labeling, color previews, and ensure controls are accessible via mouse/touch.

## 8. Testing Guidelines
- Verify painting accuracy across pitch/volume ranges in creative mode.
- Confirm tool switching updates UI and functionality without delay.
- Stress-test undo stack: continuous paint/erase operations, rapid toggling, and history size limits.
- Ensure saved PNGs match on-screen canvas, and sharing succeeds per platform target.
- Validate settings updates at runtime through Inspector adjustments.

## 9. Deliverables
- Implemented scripts and ScriptableObjects as defined above.
- Prefabs/UI layouts updated for creative mode entry point.
- Documentation covering default settings, customization steps, and designer workflows.


