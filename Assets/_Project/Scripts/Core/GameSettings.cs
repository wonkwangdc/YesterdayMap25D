using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YesterdayMap.Core
{
    public static class GameSettings
    {
        private const string VolumeKey = "settings.masterVolume";
        private const string FullScreenKey = "settings.fullScreen";
        private const string DisplayModeKey = "settings.displayMode";
        private const string ResolutionKey = "settings.resolution";
        private const string MouseSensitivityKey = "settings.mouseSensitivity";
        private const string MovementPresetKey = "settings.movementPreset";
        private const string ReduceFlashingKey = "settings.reduceFlashing";

        public const float MinimumMouseSensitivity = 0.03f;
        public const float MaximumMouseSensitivity = 0.40f;

        public static readonly Vector2Int[] Resolutions =
        {
            new(1920, 1080),
            new(2560, 1440),
            new(1600, 900),
            new(1366, 768),
            new(1280, 720),
            new(1920, 1200),
            new(2560, 1080)
        };

        public static float Volume => PlayerPrefs.GetFloat(VolumeKey, 1f);
        public static int DisplayMode => PlayerPrefs.HasKey(DisplayModeKey)
            ? Mathf.Clamp(PlayerPrefs.GetInt(DisplayModeKey, 2), 0, 2)
            : (PlayerPrefs.GetInt(FullScreenKey, 1) == 1 ? 2 : 1);
        public static bool FullScreen => DisplayMode != 1;
        public static float MouseSensitivity => Mathf.Clamp(
            PlayerPrefs.GetFloat(MouseSensitivityKey, 0.12f),
            MinimumMouseSensitivity,
            MaximumMouseSensitivity);
        public static int MovementPreset => Mathf.Clamp(
            PlayerPrefs.GetInt(MovementPresetKey, 0), 0, 1);
        public static int ResolutionIndex => Mathf.Clamp(
            PlayerPrefs.GetInt(ResolutionKey, 0),
            0,
            Resolutions.Length - 1);
        public static bool ReduceFlashing =>
            PlayerPrefs.GetInt(ReduceFlashingKey, 0) == 1;

        public static event Action<bool> ReduceFlashingChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void ApplySavedSettings()
        {
            ApplyVolume(Volume);
            ApplyDisplay(DisplayMode, ResolutionIndex);
        }

        public static void SetVolume(float value)
        {
            value = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, value);
            ApplyVolume(value);
            PlayerPrefs.Save();
        }

        public static void SetFullScreen(bool value)
        {
            SetDisplayMode(value ? 2 : 1);
        }

        public static void SetDisplayMode(int value)
        {
            value = Mathf.Clamp(value, 0, 2);
            PlayerPrefs.SetInt(DisplayModeKey, value);
            PlayerPrefs.SetInt(FullScreenKey, value == 1 ? 0 : 1);
            ApplyDisplay(value, ResolutionIndex);
            PlayerPrefs.Save();
        }

        public static void SetResolution(int index)
        {
            index = Mathf.Clamp(index, 0, Resolutions.Length - 1);
            PlayerPrefs.SetInt(ResolutionKey, index);
            ApplyDisplay(DisplayMode, index);
            PlayerPrefs.Save();
        }

        public static void SetMouseSensitivity(float value)
        {
            PlayerPrefs.SetFloat(
                MouseSensitivityKey,
                Mathf.Clamp(value, MinimumMouseSensitivity, MaximumMouseSensitivity));
            PlayerPrefs.Save();
        }

        public static void SetMovementPreset(int value)
        {
            PlayerPrefs.SetInt(MovementPresetKey, Mathf.Clamp(value, 0, 1));
            PlayerPrefs.Save();
        }

        public static void SetReduceFlashing(bool value)
        {
            if (ReduceFlashing == value)
                return;

            PlayerPrefs.SetInt(ReduceFlashingKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ReduceFlashingChanged?.Invoke(value);
        }

        // Keeps movement bindings in one place so the settings menu and player controller agree.
        public static Vector2 ReadMovementInput(Keyboard keyboard)
        {
            if (keyboard == null) return Vector2.zero;

            if (MovementPreset == 1)
            {
                return new Vector2(
                    (keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.leftArrowKey.isPressed ? 1f : 0f),
                    (keyboard.upArrowKey.isPressed ? 1f : 0f) -
                    (keyboard.downArrowKey.isPressed ? 1f : 0f));
            }

            return new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) -
                (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) -
                (keyboard.sKey.isPressed ? 1f : 0f));
        }

        private static void ApplyVolume(float value)
        {
            AudioListener.volume = Mathf.Clamp01(value);
        }

        private static void ApplyDisplay(int displayMode, int resolutionIndex)
        {
            Vector2Int resolution = Resolutions[Mathf.Clamp(
                resolutionIndex,
                0,
                Resolutions.Length - 1)];

            if (Application.isEditor)
                return;

            Screen.SetResolution(
                resolution.x,
                resolution.y,
                displayMode switch
                {
                    0 => FullScreenMode.ExclusiveFullScreen,
                    1 => FullScreenMode.Windowed,
                    _ => FullScreenMode.FullScreenWindow
                });
        }
    }
}
