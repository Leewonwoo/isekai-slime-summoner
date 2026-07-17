using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CrossDefense.Units
{
    /// <summary>필드 클릭 공격과 유닛 드래그를 한 포인터 상태 머신에서 분리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatInputController : MonoBehaviour
    {
        enum ActivePointerSource
        {
            None,
            Mouse,
            Touch,
        }

        public const float DragStartThreshold = 22f;

        Core.GameManager _gameManager;
        SummonedUnitManager _unitManager;
        SummonerAttackController _summonerAttack;
        Camera _camera;
        UIDocument _uiDocument;
        Vector2 _pressScreenPosition;
        Vector3 _pressWorldPosition;
        Vector3 _dragGrabOffset;
        SummonedUnitController _pressedUnit;
        MonsterController _pressedMonster;
        bool _pointerDown;
        bool _fieldDrag;
        bool _blockedByUi;
        ActivePointerSource _activePointerSource;

        public void Initialize(
            Core.GameManager gameManager,
            SummonedUnitManager unitManager,
            SummonerAttackController summonerAttack)
        {
            _gameManager = gameManager;
            _unitManager = unitManager;
            _summonerAttack = summonerAttack;
            _camera = Camera.main;
            _uiDocument = FindFirstObjectByType<UIDocument>();
        }

        void Update()
        {
            if (_camera == null || _unitManager == null) return;

            if (!_pointerDown)
            {
                if (TryGetPressedPointer(out Vector2 pressedScreen, out ActivePointerSource source))
                {
                    _activePointerSource = source;
                    HandlePress(pressedScreen);
                }
                return;
            }

            if (!TryGetActivePointerState(out Vector2 screen, out bool isPressed, out bool wasReleased))
            {
                CancelActivePointer();
                return;
            }

            if (wasReleased)
                HandleRelease(screen);
            else if (isPressed)
                HandleMove(screen);
        }

        void OnDisable() => CancelActivePointer();

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                CancelActivePointer();
        }

        void HandlePress(Vector2 screen)
        {
            _pointerDown = true;
            _pressScreenPosition = screen;
            _blockedByUi = IsBlockedByUi(screen);
            _fieldDrag = false;
            _dragGrabOffset = Vector3.zero;
            _pressedUnit = null;
            _pressedMonster = null;
            if (_blockedByUi) return;

            _pressWorldPosition = ScreenToWorld(screen);
            foreach (var hit in Physics2D.OverlapPointAll(_pressWorldPosition))
            {
                if (_pressedUnit == null && hit.TryGetComponent<SummonedUnitController>(out var unit))
                {
                    _pressedUnit = unit;
                    _dragGrabOffset = unit.transform.position - _pressWorldPosition;
                    _dragGrabOffset.z = 0f;
                    continue;
                }
                if (_pressedMonster == null && hit.TryGetComponent<MonsterController>(out var monster))
                    _pressedMonster = monster;
            }
        }

        void HandleMove(Vector2 screen)
        {
            if (_blockedByUi) return;
            if (!_fieldDrag && _pressedUnit != null &&
                HasExceededDragThreshold(_pressScreenPosition, screen))
                _fieldDrag = _unitManager.BeginFieldDrag(_pressedUnit);
            if (_fieldDrag)
                _unitManager.UpdateFieldDrag(GetDragWorldPosition(ScreenToWorld(screen), _dragGrabOffset));
        }

        void HandleRelease(Vector2 screen)
        {
            _pointerDown = false;
            if (_blockedByUi)
            {
                ResetPointerState();
                return;
            }

            Vector3 world = ScreenToWorld(screen);
            if (_fieldDrag)
            {
                _unitManager.EndFieldDrag(
                    GetDragWorldPosition(world, _dragGrabOffset),
                    _unitManager.IsScreenPositionInField(screen));
                ResetPointerState();
                return;
            }

            if (!HasExceededDragThreshold(_pressScreenPosition, screen))
                _summonerAttack?.TryClickAttack(world, _pressedMonster);
            ResetPointerState();
        }

        public static bool HasExceededDragThreshold(Vector2 pressPosition, Vector2 currentPosition) =>
            (currentPosition - pressPosition).sqrMagnitude >= DragStartThreshold * DragStartThreshold;

        public static Vector3 GetDragWorldPosition(Vector3 pointerWorldPosition, Vector3 grabOffset)
        {
            Vector3 position = pointerWorldPosition + grabOffset;
            position.z = 0f;
            return position;
        }

        public static Vector2 GetPanelInputPosition(Vector2 screenPosition, float screenHeight) =>
            new(screenPosition.x, Mathf.Max(0f, screenHeight) - screenPosition.y);

        static bool TryGetPressedPointer(out Vector2 screen, out ActivePointerSource source)
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.primaryTouch.press.wasPressedThisFrame)
            {
                screen = touchscreen.primaryTouch.position.ReadValue();
                source = ActivePointerSource.Touch;
                return true;
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                screen = mouse.position.ReadValue();
                source = ActivePointerSource.Mouse;
                return true;
            }

            screen = default;
            source = ActivePointerSource.None;
            return false;
        }

        bool TryGetActivePointerState(out Vector2 screen, out bool isPressed, out bool wasReleased)
        {
            if (_activePointerSource == ActivePointerSource.Touch)
            {
                var touchscreen = Touchscreen.current;
                if (touchscreen != null)
                {
                    screen = touchscreen.primaryTouch.position.ReadValue();
                    isPressed = touchscreen.primaryTouch.press.isPressed;
                    wasReleased = touchscreen.primaryTouch.press.wasReleasedThisFrame;
                    return true;
                }
            }
            else if (_activePointerSource == ActivePointerSource.Mouse)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    screen = mouse.position.ReadValue();
                    isPressed = mouse.leftButton.isPressed;
                    wasReleased = mouse.leftButton.wasReleasedThisFrame;
                    return true;
                }
            }

            screen = default;
            isPressed = false;
            wasReleased = false;
            return false;
        }

        bool IsBlockedByUi(Vector2 screen)
        {
            if (_uiDocument == null) return false;
            var root = _uiDocument.rootVisualElement;
            var panel = root?.panel;
            if (panel == null) return false;

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(
                panel,
                GetPanelInputPosition(screen, Screen.height));
            var picked = panel.Pick(panelPosition);
            while (picked != null)
            {
                if (picked.name == "zone-bottom" || picked.name == "zone-top" ||
                    picked.name == "summon-modal-overlay" || picked.name == "unit-detail-overlay" ||
                    picked is Button)
                    return true;
                picked = picked.parent;
            }
            return false;
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -_camera.transform.position.z));
            world.z = 0f;
            return world;
        }

        void ResetPointerState()
        {
            _blockedByUi = false;
            _fieldDrag = false;
            _dragGrabOffset = Vector3.zero;
            _activePointerSource = ActivePointerSource.None;
            _pressedUnit = null;
            _pressedMonster = null;
        }

        void CancelActivePointer()
        {
            if (_fieldDrag)
                _unitManager?.CancelFieldDrag();
            _pointerDown = false;
            ResetPointerState();
        }
    }
}
