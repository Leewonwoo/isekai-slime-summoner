using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace CrossDefense.Units
{
    /// <summary>필드 클릭 공격과 유닛 드래그를 한 포인터 상태 머신에서 분리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatInputController : MonoBehaviour
    {
        const float ClickMoveThreshold = 22f;

        Core.GameManager _gameManager;
        SummonedUnitManager _unitManager;
        SummonerAttackController _summonerAttack;
        Camera _camera;
        UIDocument _uiDocument;
        Vector2 _pressScreenPosition;
        Vector3 _pressWorldPosition;
        SummonedUnitController _pressedUnit;
        MonsterController _pressedMonster;
        bool _pointerDown;
        bool _fieldDrag;
        bool _blockedByUi;

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
            var pointer = Pointer.current;
            if (pointer == null || _camera == null || _unitManager == null) return;
            Vector2 screen = pointer.position.ReadValue();

            if (pointer.press.wasPressedThisFrame)
                HandlePress(screen);
            else if (_pointerDown && pointer.press.isPressed)
                HandleMove(screen);
            else if (_pointerDown && pointer.press.wasReleasedThisFrame)
                HandleRelease(screen);
        }

        void HandlePress(Vector2 screen)
        {
            _pointerDown = true;
            _pressScreenPosition = screen;
            _blockedByUi = IsBlockedByUi(screen);
            _fieldDrag = false;
            _pressedUnit = null;
            _pressedMonster = null;
            if (_blockedByUi) return;

            _pressWorldPosition = ScreenToWorld(screen);
            foreach (var hit in Physics2D.OverlapPointAll(_pressWorldPosition))
            {
                if (_pressedUnit == null && hit.TryGetComponent<SummonedUnitController>(out var unit))
                {
                    _pressedUnit = unit;
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
                Vector2.Distance(screen, _pressScreenPosition) >= ClickMoveThreshold)
                _fieldDrag = _unitManager.BeginFieldDrag(_pressedUnit);
            if (_fieldDrag)
                _unitManager.UpdateFieldDrag(ScreenToWorld(screen));
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
                _unitManager.EndFieldDrag(world, _unitManager.IsScreenPositionInField(screen));
                ResetPointerState();
                return;
            }

            if (Vector2.Distance(screen, _pressScreenPosition) <= ClickMoveThreshold)
                _summonerAttack?.TryClickAttack(world, _pressedMonster);
            ResetPointerState();
        }

        bool IsBlockedByUi(Vector2 screen)
        {
            if (_uiDocument == null) return false;
            var root = _uiDocument.rootVisualElement;
            var panel = root?.panel;
            if (panel == null) return false;

            Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screen);
            var picked = panel.Pick(panelPosition);
            while (picked != null)
            {
                if (picked.name == "zone-bottom" || picked.name == "zone-top" ||
                    picked.name == "summon-modal-overlay" || picked is Button)
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
            _pressedUnit = null;
            _pressedMonster = null;
        }
    }
}
