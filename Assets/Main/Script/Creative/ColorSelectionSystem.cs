using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クリエイティブモードの色選択を管理するシステム
/// UIやボイス入力から呼び出され、CreativeModeManagerへ色を反映する
/// </summary>
public class ColorSelectionSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private ColorSelectionSettings settings;

    [Header("References")]
    [SerializeField] private CreativeModeManager creativeModeManager;

    [Header("Runtime (Read Only)")]
    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private int currentIndex = -1;

    public event Action<Color> OnColorSelected;
    public event Action<int> OnColorIndexChanged;

    bool suppressManagerCallback = false;
    
    public ColorSelectionSettings Settings => settings;

    void Awake()
    {
        ResolveReferences();
        InitializeColor();
    }

    void OnEnable()
    {
        CreativeModeManager.OnColorChanged += HandleManagerColorChanged;
    }

    void OnDisable()
    {
        CreativeModeManager.OnColorChanged -= HandleManagerColorChanged;
    }

    void ResolveReferences()
    {
        if (creativeModeManager == null)
        {
            creativeModeManager = FindObjectOfType<CreativeModeManager>();
        }

        if (settings == null)
        {
            Debug.LogWarning("ColorSelectionSystem: ColorSelectionSettingsが設定されていません。デフォルト設定を使用します。");
            settings = ScriptableObject.CreateInstance<ColorSelectionSettings>();
        }
    }

    void InitializeColor()
    {
        if (creativeModeManager != null)
        {
            currentColor = creativeModeManager.CurrentColor;
        }
        else if (settings != null && settings.presetColors.Length > 0)
        {
            currentColor = settings.presetColors[0];
        }
        else
        {
            currentColor = Color.white;
        }

        UpdateCurrentIndexFromColor(currentColor);
        RaiseColorEvents();
    }

    void HandleManagerColorChanged(Color color)
    {
        if (suppressManagerCallback)
        {
            return;
        }

        currentColor = color;
        UpdateCurrentIndexFromColor(color);
        RaiseColorEvents();
    }

    void UpdateCurrentIndexFromColor(Color color)
    {
        var palette = GetAvailableColors();
        for (int i = 0; i < palette.Count; i++)
        {
            if (ApproximatelyEqualColor(palette[i], color))
            {
                currentIndex = i;
                OnColorIndexChanged?.Invoke(currentIndex);
                return;
            }
        }

        currentIndex = -1;
        OnColorIndexChanged?.Invoke(currentIndex);
    }

    bool ApproximatelyEqualColor(Color a, Color b)
    {
        const float epsilon = 0.01f;
        return Mathf.Abs(a.r - b.r) < epsilon &&
               Mathf.Abs(a.g - b.g) < epsilon &&
               Mathf.Abs(a.b - b.b) < epsilon &&
               Mathf.Abs(a.a - b.a) < epsilon;
    }

    void RaiseColorEvents()
    {
        OnColorSelected?.Invoke(currentColor);
    }

    public IReadOnlyList<Color> GetAvailableColors()
    {
        if (settings != null && settings.presetColors != null && settings.presetColors.Length > 0)
        {
            return settings.presetColors;
        }

        return Array.Empty<Color>();
    }

    public void SelectColorByIndex(int index)
    {
        var palette = GetAvailableColors();
        if (palette.Count == 0)
        {
            return;
        }

        index = Mathf.Clamp(index, 0, palette.Count - 1);
        SelectColor(palette[index]);
    }

    public void SelectNextColor()
    {
        var palette = GetAvailableColors();
        if (palette.Count == 0)
        {
            return;
        }

        int nextIndex = (currentIndex + 1 + palette.Count) % palette.Count;
        SelectColor(palette[nextIndex]);
    }

    public void SelectPreviousColor()
    {
        var palette = GetAvailableColors();
        if (palette.Count == 0)
        {
            return;
        }

        int prevIndex = (currentIndex - 1 + palette.Count) % palette.Count;
        SelectColor(palette[prevIndex]);
    }

    public void SelectColor(Color color)
    {
        currentColor = color;
        UpdateCurrentIndexFromColor(color);
        RaiseColorEvents();

        if (creativeModeManager != null)
        {
            suppressManagerCallback = true;
            creativeModeManager.SetColor(color);
            suppressManagerCallback = false;
        }
    }

    /// <summary>
    /// 音声ピッチによる色選択。未設定の場合は最も近い区間を使用。
    /// </summary>
    public void SelectColorByPitch(float pitch)
    {
        if (settings == null || settings.voicePitchThresholds == null || settings.voicePitchThresholds.Length == 0)
        {
            return;
        }

        int index = 0;
        for (int i = 0; i < settings.voicePitchThresholds.Length; i++)
        {
            if (pitch >= settings.voicePitchThresholds[i])
            {
                index = i;
            }
        }

        SelectColorByIndex(index);
    }

    public Color GetCurrentColor() => currentColor;
    public int GetCurrentIndex() => currentIndex;
}

