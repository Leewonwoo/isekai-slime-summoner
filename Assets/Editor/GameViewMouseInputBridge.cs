using UnityEditor;
using UnityEngine.InputSystem;

namespace CrossDefense.Editor
{
    /// <summary>
    /// Device Simulator가 비활성화한 네이티브 마우스를 Game 뷰 포커스에서 복구한다.
    /// Simulator로 돌아가면 중복 포인터를 피하기 위해 다시 비활성화한다.
    /// </summary>
    [InitializeOnLoad]
    public static class GameViewMouseInputBridge
    {
        const string GameViewTypeName = "UnityEditor.GameView";
        const string SimulatorWindowTypeName = "UnityEditor.DeviceSimulation.SimulatorWindow";
        static EditorWindow _lastFocusedWindow;

        static GameViewMouseInputBridge()
        {
            EditorApplication.update -= UpdateInputDeviceForFocusedWindow;
            EditorApplication.update += UpdateInputDeviceForFocusedWindow;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void UpdateInputDeviceForFocusedWindow()
        {
            if (!EditorApplication.isPlaying)
            {
                _lastFocusedWindow = null;
                return;
            }

            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow == null || focusedWindow == _lastFocusedWindow)
                return;
            _lastFocusedWindow = focusedWindow;

            string typeName = focusedWindow.GetType().FullName;
            if (typeName == GameViewTypeName)
                SetNativeMouseEnabled(true);
            else if (typeName == SimulatorWindowTypeName)
                SetNativeMouseEnabled(false);
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            _lastFocusedWindow = null;
            if (state != PlayModeStateChange.EnteredPlayMode)
                return;

            // Device Simulator can leave the native mouse disabled between play sessions.
            // Enable it first, then let the focused Simulator window disable it if needed.
            SetNativeMouseEnabled(true);
            EditorApplication.delayCall += UpdateInputDeviceForFocusedWindow;
        }

        static void SetNativeMouseEnabled(bool enabled)
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is not Mouse || !device.native || device.enabled == enabled)
                    continue;
                if (enabled)
                    InputSystem.EnableDevice(device);
                else
                    InputSystem.DisableDevice(device);
            }
        }
    }
}
