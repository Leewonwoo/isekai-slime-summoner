using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CrossDefense.Editor
{
    /// <summary>
    /// Validates fixed-grid character animation sheets and lets artists translate frame pixels
    /// inside each cell. Existing Sprite IDs are reused whenever a frame can be matched.
    /// </summary>
    public sealed class AnimationSheetAlignmentEditorWindow : EditorWindow
    {
        const int DefaultColumns = 3;
        const int DefaultRows = 3;
        const int DefaultCellSize = 128;
        const float DefaultAnimationFps = 9f;
        const float PreviewMaxSize = 420f;
        const byte VisibleAlphaThreshold = 0;
        const string BackupFolderName = "CrossDefenseAnimationBackups";

        readonly List<ValidationIssue> _issues = new();

        Texture2D _sheet;
        int _columns = DefaultColumns;
        int _rows = DefaultRows;
        int _cellSize = DefaultCellSize;
        float _expectedPixelsPerUnit = 200f;
        Vector2 _contentAnchor = new(64f, 112f);
        float _previewFps = DefaultAnimationFps;
        int _frameIndex;
        bool _isPlaying = true;
        bool _showOnionSkin = true;
        int _referenceFrameIndex;
        double _lastFrameTime;
        Vector2 _mainScroll;
        Vector2 _validationScroll;
        bool _validationDirty = true;
        Vector2Int[] _frameOffsets = Array.Empty<Vector2Int>();
        RectInt[] _frameAlphaBounds = Array.Empty<RectInt>();
        Color32[] _sourcePixels;
        int _sourceWidth;
        int _sourceHeight;
        string _pixelLoadError;

        int FrameCount => Mathf.Max(1, _columns * _rows);
        bool HasPendingOffsets => _frameOffsets.Any(offset => offset != Vector2Int.zero);

        [MenuItem("Isekai Slime Summoner/Animation Sheet Alignment", priority = 1)]
        public static void OpenWindow()
        {
            var window = GetWindow<AnimationSheetAlignmentEditorWindow>();
            window.titleContent = new GUIContent("Sheet Alignment");
            window.minSize = new Vector2(860f, 760f);
            window.TryUseSelection();
            window.Show();
        }

        void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
            _lastFrameTime = EditorApplication.timeSinceStartup;
            TryUseSelection();
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        void OnSelectionChange()
        {
            if (TryResolveTexture(Selection.activeObject, out var selectedTexture))
            {
                SetSheet(selectedTexture);
                Repaint();
            }
        }

        void OnEditorUpdate()
        {
            if (!_isPlaying || _sheet == null || _previewFps <= 0f)
                return;

            double now = EditorApplication.timeSinceStartup;
            double frameDuration = 1d / _previewFps;
            if (now - _lastFrameTime < frameDuration)
                return;

            _frameIndex = (_frameIndex + 1) % FrameCount;
            _lastFrameTime = now;
            Repaint();
        }

        void OnGUI()
        {
            HandleNudgeKeyboard();
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            DrawHeader();
            DrawConfiguration();
            EditorGUILayout.Space(8f);

            EditorGUILayout.BeginHorizontal();
            DrawPreviewColumn();
            DrawValidationColumn();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            EditorGUILayout.LabelField("Animation Sheet Alignment", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "고정 Grid·Center 피봇을 검사하고, 프레임별 위치를 미리 조정한 뒤 처리본 PNG에 저장합니다. ArtSource 원본은 유지됩니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            var nextSheet = (Texture2D)EditorGUILayout.ObjectField("Sprite Sheet", _sheet, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
                SetSheet(nextSheet);
        }

        void DrawConfiguration()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Project Preset", EditorStyles.boldLabel);
            if (GUILayout.Button("3×3 / 128px 규격 복원", GUILayout.Width(170f)))
                ApplyProjectPreset();
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            _columns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _columns));
            _rows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _rows));
            _cellSize = Mathf.Max(1, EditorGUILayout.IntField("Cell Size", _cellSize));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _expectedPixelsPerUnit = Mathf.Max(1f, EditorGUILayout.FloatField("Expected PPU", _expectedPixelsPerUnit));
            _contentAnchor = EditorGUILayout.Vector2Field("Content Anchor (top-left px)", _contentAnchor);
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                _frameIndex = Mathf.Clamp(_frameIndex, 0, FrameCount - 1);
                _referenceFrameIndex = Mathf.Clamp(_referenceFrameIndex, 0, FrameCount - 1);
                EnsureFrameBuffers();
                LoadSourcePixels();
                _validationDirty = true;
            }

            EditorGUILayout.LabelField(
                "Content Anchor는 미리보기 가이드입니다. Unity 피봇은 프로젝트 규격에 따라 모든 프레임을 Center로 통일합니다.",
                EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        void DrawPreviewColumn()
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(440f), GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Frame Preview", EditorStyles.boldLabel);

            Rect availableRect = GUILayoutUtility.GetRect(
                PreviewMaxSize,
                PreviewMaxSize,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(PreviewMaxSize));
            float previewSize = Mathf.Min(availableRect.width, availableRect.height);
            var previewRect = new Rect(
                availableRect.x + (availableRect.width - previewSize) * 0.5f,
                availableRect.y,
                previewSize,
                previewSize);

            DrawFramePreview(previewRect);
            DrawPlaybackControls();
            DrawNudgeControls();
            EditorGUILayout.EndVertical();
        }

        void DrawFramePreview(Rect previewRect)
        {
            EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f, 1f));
            if (_sheet == null)
            {
                GUI.Label(previewRect, "Project 창에서 *_sheet.png를 선택하세요.", CenteredLabelStyle());
                return;
            }

            int referenceFrame = Mathf.Clamp(_referenceFrameIndex, 0, FrameCount - 1);
            if (_showOnionSkin && referenceFrame != _frameIndex)
                DrawFrameTexture(previewRect, referenceFrame, 0.28f);

            DrawFrameTexture(previewRect, _frameIndex, 1f);
            DrawAnchorGuide(previewRect);

            Vector2Int offset = GetFrameOffset(_frameIndex);
            var labelRect = new Rect(previewRect.x + 8f, previewRect.y + 8f, 250f, 22f);
            GUI.Label(
                labelRect,
                $"Frame {_frameIndex + 1}/{FrameCount}  Offset ({offset.x}, {offset.y})",
                EditorStyles.whiteLabel);
        }

        void DrawFrameTexture(Rect previewRect, int frameIndex, float alpha)
        {
            if (_sheet == null || _cellSize <= 0 || _columns <= 0 || _rows <= 0)
                return;

            int column = frameIndex % _columns;
            int rowFromTop = frameIndex / _columns;
            float pixelX = column * _cellSize;
            float pixelY = _sheet.height - ((rowFromTop + 1) * _cellSize);
            if (pixelX < 0f || pixelY < 0f || pixelX + _cellSize > _sheet.width || pixelY + _cellSize > _sheet.height)
                return;

            var texCoords = new Rect(
                pixelX / _sheet.width,
                pixelY / _sheet.height,
                (float)_cellSize / _sheet.width,
                (float)_cellSize / _sheet.height);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alpha);
            Vector2Int offset = GetFrameOffset(frameIndex);
            float pixelScale = previewRect.width / _cellSize;
            GUI.BeginClip(previewRect);
            var shiftedRect = new Rect(
                offset.x * pixelScale,
                offset.y * pixelScale,
                previewRect.width,
                previewRect.height);
            GUI.DrawTextureWithTexCoords(shiftedRect, _sheet, texCoords, true);
            GUI.EndClip();
            GUI.color = previousColor;
        }

        void DrawAnchorGuide(Rect previewRect)
        {
            float anchorX = previewRect.x + (_contentAnchor.x / _cellSize) * previewRect.width;
            float anchorY = previewRect.y + (_contentAnchor.y / _cellSize) * previewRect.height;

            Handles.BeginGUI();
            Color previousColor = Handles.color;
            Handles.color = new Color(0.2f, 0.95f, 0.4f, 0.9f);
            Handles.DrawLine(new Vector3(anchorX, previewRect.y), new Vector3(anchorX, previewRect.yMax));
            Handles.DrawLine(new Vector3(previewRect.x, anchorY), new Vector3(previewRect.xMax, anchorY));
            Handles.DrawWireDisc(new Vector3(anchorX, anchorY), Vector3.forward, 5f);
            Handles.color = previousColor;
            Handles.EndGUI();
        }

        void DrawPlaybackControls()
        {
            using (new EditorGUI.DisabledScope(_sheet == null))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(36f)))
                    StepFrame(-1);
                if (GUILayout.Button(_isPlaying ? "Pause" : "Play", EditorStyles.toolbarButton, GUILayout.Width(58f)))
                {
                    _isPlaying = !_isPlaying;
                    _lastFrameTime = EditorApplication.timeSinceStartup;
                }
                if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(36f)))
                    StepFrame(1);

                _showOnionSkin = GUILayout.Toggle(_showOnionSkin, "Onion Skin", EditorStyles.toolbarButton, GUILayout.Width(92f));
                GUILayout.FlexibleSpace();
                GUILayout.Label("FPS", GUILayout.Width(26f));
                _previewFps = EditorGUILayout.Slider(_previewFps, 1f, 24f, GUILayout.Width(150f));
                EditorGUILayout.EndHorizontal();

                int displayedFrame = _frameIndex + 1;
                EditorGUI.BeginChangeCheck();
                displayedFrame = EditorGUILayout.IntSlider("Frame", displayedFrame, 1, FrameCount);
                if (EditorGUI.EndChangeCheck())
                {
                    _frameIndex = displayedFrame - 1;
                    _isPlaying = false;
                    Repaint();
                }

                int displayedReference = _referenceFrameIndex + 1;
                EditorGUI.BeginChangeCheck();
                displayedReference = EditorGUILayout.IntSlider("Reference", displayedReference, 1, FrameCount);
                if (EditorGUI.EndChangeCheck())
                {
                    _referenceFrameIndex = displayedReference - 1;
                    _isPlaying = false;
                    Repaint();
                }
            }
        }

        void DrawNudgeControls()
        {
            EnsureFrameBuffers();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Frame Pixel Position", EditorStyles.boldLabel);

            Vector2Int currentOffset = GetFrameOffset(_frameIndex);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            int offsetX = EditorGUILayout.IntField("X (right +)", currentOffset.x);
            int offsetY = EditorGUILayout.IntField("Y (down +)", currentOffset.y);
            bool resetFrame = GUILayout.Button("Reset Frame", GUILayout.Width(92f));
            if (resetFrame)
            {
                offsetX = 0;
                offsetY = 0;
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck() || resetFrame)
                SetFrameOffset(_frameIndex, new Vector2Int(offsetX, offsetY));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("←", GUILayout.Width(42f)))
                NudgeCurrentFrame(-1, 0);
            if (GUILayout.Button("↑", GUILayout.Width(42f)))
                NudgeCurrentFrame(0, -1);
            if (GUILayout.Button("↓", GUILayout.Width(42f)))
                NudgeCurrentFrame(0, 1);
            if (GUILayout.Button("→", GUILayout.Width(42f)))
                NudgeCurrentFrame(1, 0);
            if (GUILayout.Button("Reset All", GUILayout.Width(78f)))
                ResetAllOffsets();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(
                "방향키: 1px 이동 · Shift+방향키: 4px 이동 · Reference 프레임은 반투명 고정 기준",
                EditorStyles.wordWrappedMiniLabel);

            int clippingCount = CountClippingFrames();
            if (!string.IsNullOrEmpty(_pixelLoadError))
                EditorGUILayout.HelpBox(_pixelLoadError, MessageType.Error);
            else if (clippingCount > 0)
                EditorGUILayout.HelpBox($"{clippingCount}개 프레임이 셀 경계를 넘어 잘립니다. 위치를 안쪽으로 옮겨주세요.", MessageType.Error);

            using (new EditorGUI.DisabledScope(!CanSaveAlignedPng()))
            {
                if (GUILayout.Button("위치 보정 PNG 저장", GUILayout.Height(30f)))
                    SaveAlignedPng();
            }

            if (!HasPendingOffsets)
                EditorGUILayout.LabelField("저장할 위치 변경이 없습니다.", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        void DrawValidationColumn()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(350f), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            EnsureValidation();

            _validationScroll = EditorGUILayout.BeginScrollView(_validationScroll, EditorStyles.helpBox, GUILayout.Height(420f));
            if (_sheet == null)
            {
                EditorGUILayout.HelpBox("검사할 Sprite Sheet가 없습니다.", MessageType.Info);
            }
            else if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("현재 시트가 프로젝트 애니메이션 규격을 충족합니다.", MessageType.Info);
            }
            else
            {
                foreach (ValidationIssue issue in _issues)
                    EditorGUILayout.HelpBox(issue.Message, issue.Type);
            }
            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(!CanApplyStandards()))
            {
                if (GUILayout.Button("고정 Grid + Import Settings 적용", GUILayout.Height(34f)))
                    ApplyStandards();
            }

            if (_sheet != null && !CanApplyStandards())
                EditorGUILayout.HelpBox("텍스처 크기가 Grid 규격과 정확히 일치해야 적용할 수 있습니다.", MessageType.Warning);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("안전 동작", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• 저장 전 셀 경계 클리핑 검사", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• 저장 전 Library에 PNG 자동 백업", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• 픽셀 저장은 ArtSource 원본과 Sprite ID 유지", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• 기존 Sprite ID를 이름/셀 위치로 재사용", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("• 남는 Sprite Rect는 적용 전 확인 후 제거", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        void ApplyProjectPreset()
        {
            _columns = DefaultColumns;
            _rows = DefaultRows;
            _cellSize = DefaultCellSize;
            _contentAnchor = new Vector2(64f, 112f);
            _expectedPixelsPerUnit = InferExpectedPixelsPerUnit(GetAssetPath());
            _frameIndex = Mathf.Clamp(_frameIndex, 0, FrameCount - 1);
            _referenceFrameIndex = Mathf.Clamp(_referenceFrameIndex, 0, FrameCount - 1);
            EnsureFrameBuffers();
            LoadSourcePixels();
            _validationDirty = true;
            Repaint();
        }

        void StepFrame(int delta)
        {
            _isPlaying = false;
            _frameIndex = (_frameIndex + delta + FrameCount) % FrameCount;
            Repaint();
        }

        bool TryUseSelection()
        {
            if (!TryResolveTexture(Selection.activeObject, out var selectedTexture))
                return false;

            SetSheet(selectedTexture);
            return true;
        }

        void SetSheet(Texture2D sheet)
        {
            if (_sheet == sheet)
                return;

            _sheet = sheet;
            _frameIndex = 0;
            _referenceFrameIndex = 0;
            _lastFrameTime = EditorApplication.timeSinceStartup;
            _expectedPixelsPerUnit = InferExpectedPixelsPerUnit(GetAssetPath());
            _frameOffsets = new Vector2Int[FrameCount];
            _frameAlphaBounds = new RectInt[FrameCount];
            LoadSourcePixels();
            _validationDirty = true;
        }

        static bool TryResolveTexture(UnityEngine.Object selectedObject, out Texture2D texture)
        {
            switch (selectedObject)
            {
                case Texture2D selectedTexture:
                    texture = selectedTexture;
                    return true;
                case Sprite selectedSprite:
                    texture = selectedSprite.texture;
                    return texture != null;
                default:
                    texture = null;
                    return false;
            }
        }

        void EnsureValidation()
        {
            if (!_validationDirty)
                return;

            _validationDirty = false;
            _issues.Clear();
            if (_sheet == null)
                return;

            string path = GetAssetPath();
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                AddIssue(MessageType.Error, "선택한 에셋의 TextureImporter를 찾을 수 없습니다.");
                return;
            }

            int expectedWidth = _columns * _cellSize;
            int expectedHeight = _rows * _cellSize;
            if (_sheet.width != expectedWidth || _sheet.height != expectedHeight)
                AddIssue(MessageType.Error, $"Texture 크기: {_sheet.width}×{_sheet.height} (필요: {expectedWidth}×{expectedHeight})");

            if (!string.IsNullOrEmpty(_pixelLoadError))
                AddIssue(MessageType.Error, _pixelLoadError);
            int clippingCount = CountClippingFrames();
            if (clippingCount > 0)
                AddIssue(MessageType.Error, $"{clippingCount}개 프레임의 보정 위치가 셀 경계를 넘어갑니다.");
            else if (HasPendingOffsets)
                AddIssue(MessageType.Info, "저장되지 않은 프레임 위치 보정이 있습니다.");

            if (_contentAnchor.x < 0f || _contentAnchor.y < 0f ||
                _contentAnchor.x > _cellSize || _contentAnchor.y > _cellSize)
                AddIssue(MessageType.Warning, "Content Anchor가 셀 영역 밖에 있습니다.");

            if (importer.textureType != TextureImporterType.Sprite)
                AddIssue(MessageType.Warning, "Texture Type이 Sprite (2D and UI)가 아닙니다.");
            if (importer.spriteImportMode != SpriteImportMode.Multiple)
                AddIssue(MessageType.Warning, "Sprite Mode가 Multiple이 아닙니다.");
            if (!Mathf.Approximately(importer.spritePixelsPerUnit, _expectedPixelsPerUnit))
                AddIssue(MessageType.Warning, $"PPU: {importer.spritePixelsPerUnit:0.##} (필요: {_expectedPixelsPerUnit:0.##})");
            if (importer.filterMode != FilterMode.Point)
                AddIssue(MessageType.Warning, $"Filter Mode가 {importer.filterMode}입니다. Point가 필요합니다.");
            if (importer.mipmapEnabled)
                AddIssue(MessageType.Warning, "Mip Map이 켜져 있습니다.");
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                AddIssue(MessageType.Warning, "Compression이 None이 아닙니다.");
            if (GetSpriteMeshType(importer) != SpriteMeshType.FullRect)
                AddIssue(MessageType.Warning, "Mesh Type이 Full Rect가 아닙니다.");

            ISpriteEditorDataProvider dataProvider = CreateDataProvider(importer);
            if (dataProvider == null)
            {
                AddIssue(MessageType.Error, "Sprite Editor Data Provider를 만들 수 없습니다.");
                return;
            }

            SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
            int expectedCount = _columns * _rows;
            if (spriteRects.Length != expectedCount)
                AddIssue(MessageType.Warning, $"Sprite Rect 수: {spriteRects.Length} (필요: {expectedCount})");

            List<SpriteRect> available = spriteRects.ToList();
            int rectMismatchCount = 0;
            int pivotMismatchCount = 0;
            for (int index = 0; index < expectedCount; index++)
            {
                Rect expectedRect = GetExpectedRect(index, _sheet.height);
                SpriteRect matched = TakeMatchingRect(available, index, expectedRect, false);
                if (matched == null)
                {
                    rectMismatchCount++;
                    continue;
                }

                if (!RectsApproximatelyEqual(matched.rect, expectedRect))
                    rectMismatchCount++;
                if (matched.alignment != SpriteAlignment.Center)
                    pivotMismatchCount++;
            }

            if (rectMismatchCount > 0)
                AddIssue(MessageType.Warning, $"{rectMismatchCount}개 프레임이 {_cellSize}×{_cellSize} 고정 Grid와 다릅니다.");
            if (pivotMismatchCount > 0)
                AddIssue(MessageType.Warning, $"{pivotMismatchCount}개 프레임의 피봇이 Center가 아닙니다.");
        }

        bool CanApplyStandards()
        {
            if (_sheet == null || _columns <= 0 || _rows <= 0 || _cellSize <= 0)
                return false;

            return _sheet.width == _columns * _cellSize &&
                   _sheet.height == _rows * _cellSize &&
                   AssetImporter.GetAtPath(GetAssetPath()) is TextureImporter;
        }

        void ApplyStandards()
        {
            if (!CanApplyStandards())
                return;

            string path = GetAssetPath();
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            ISpriteEditorDataProvider initialProvider = CreateDataProvider(importer);
            int oldRectCount = initialProvider?.GetSpriteRects().Length ?? 0;
            int targetCount = _columns * _rows;
            string removalWarning = oldRectCount > targetCount
                ? $"\n\n주의: 남는 Sprite Rect {oldRectCount - targetCount}개는 제거됩니다."
                : string.Empty;

            bool confirmed = EditorUtility.DisplayDialog(
                "Animation Sheet 규격 적용",
                $"{Path.GetFileName(path)}에 {targetCount}개 고정 Grid와 공통 Center 피봇을 적용합니다.\n" +
                "기존 Sprite ID는 이름과 셀 위치를 기준으로 최대한 보존합니다." + removalWarning,
                "적용",
                "취소");
            if (!confirmed)
                return;

            try
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = _expectedPixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                SetSpriteMeshType(importer, SpriteMeshType.FullRect);
                importer.SaveAndReimport();

                importer = AssetImporter.GetAtPath(path) as TextureImporter;
                ISpriteEditorDataProvider dataProvider = CreateDataProvider(importer);
                if (dataProvider == null)
                    throw new InvalidOperationException("Sprite Editor Data Provider를 만들 수 없습니다.");

                SpriteRect[] existingRects = dataProvider.GetSpriteRects();
                SpriteRect[] repairedRects = BuildFixedGridRects(existingRects, path);
                dataProvider.SetSpriteRects(repairedRects);

                ISpriteNameFileIdDataProvider nameProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
                nameProvider?.SetNameFileIdPairs(
                    repairedRects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)).ToArray());

                dataProvider.Apply();
                importer.SaveAndReimport();

                _sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                LoadSourcePixels();
                Selection.activeObject = _sheet;
                _validationDirty = true;
                EnsureValidation();
                ShowNotification(new GUIContent(_issues.Count == 0 ? "규격 적용 완료" : "적용 완료 — Validation 확인"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("적용 실패", exception.Message, "확인");
            }
        }

        void HandleNudgeKeyboard()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null ||
                currentEvent.type != EventType.KeyDown ||
                EditorGUIUtility.editingTextField ||
                _sheet == null)
                return;

            int step = currentEvent.shift ? 4 : 1;
            bool handled = true;
            switch (currentEvent.keyCode)
            {
                case KeyCode.LeftArrow:
                    NudgeCurrentFrame(-step, 0);
                    break;
                case KeyCode.RightArrow:
                    NudgeCurrentFrame(step, 0);
                    break;
                case KeyCode.UpArrow:
                    NudgeCurrentFrame(0, -step);
                    break;
                case KeyCode.DownArrow:
                    NudgeCurrentFrame(0, step);
                    break;
                default:
                    handled = false;
                    break;
            }

            if (handled)
                currentEvent.Use();
        }

        void NudgeCurrentFrame(int deltaX, int deltaY)
        {
            SetFrameOffset(_frameIndex, GetFrameOffset(_frameIndex) + new Vector2Int(deltaX, deltaY));
        }

        void SetFrameOffset(int frameIndex, Vector2Int offset)
        {
            EnsureFrameBuffers();
            if (frameIndex < 0 || frameIndex >= _frameOffsets.Length || _frameOffsets[frameIndex] == offset)
                return;

            _frameOffsets[frameIndex] = offset;
            _isPlaying = false;
            _validationDirty = true;
            Repaint();
        }

        Vector2Int GetFrameOffset(int frameIndex)
        {
            EnsureFrameBuffers();
            return frameIndex >= 0 && frameIndex < _frameOffsets.Length
                ? _frameOffsets[frameIndex]
                : Vector2Int.zero;
        }

        void ResetAllOffsets()
        {
            EnsureFrameBuffers();
            if (!HasPendingOffsets)
                return;

            Array.Clear(_frameOffsets, 0, _frameOffsets.Length);
            _validationDirty = true;
            Repaint();
        }

        void EnsureFrameBuffers()
        {
            int frameCount = FrameCount;
            if (_frameOffsets.Length != frameCount)
            {
                var resizedOffsets = new Vector2Int[frameCount];
                Array.Copy(_frameOffsets, resizedOffsets, Mathf.Min(_frameOffsets.Length, frameCount));
                _frameOffsets = resizedOffsets;
            }

            if (_frameAlphaBounds.Length != frameCount)
                _frameAlphaBounds = new RectInt[frameCount];
        }

        void LoadSourcePixels()
        {
            EnsureFrameBuffers();
            _sourcePixels = null;
            _sourceWidth = 0;
            _sourceHeight = 0;
            _pixelLoadError = null;
            Array.Clear(_frameAlphaBounds, 0, _frameAlphaBounds.Length);

            string assetPath = GetAssetPath();
            if (_sheet == null || string.IsNullOrEmpty(assetPath))
                return;
            if (!string.Equals(Path.GetExtension(assetPath), ".png", StringComparison.OrdinalIgnoreCase))
            {
                _pixelLoadError = "수동 위치 저장은 PNG 에셋만 지원합니다.";
                return;
            }

            string absolutePath = GetAbsoluteAssetPath(assetPath);
            if (!File.Exists(absolutePath))
            {
                _pixelLoadError = $"PNG 파일을 찾을 수 없습니다: {assetPath}";
                return;
            }

            Texture2D readableTexture = null;
            try
            {
                readableTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (!ImageConversion.LoadImage(readableTexture, File.ReadAllBytes(absolutePath), false))
                    throw new InvalidOperationException("PNG 디코딩에 실패했습니다.");

                _sourceWidth = readableTexture.width;
                _sourceHeight = readableTexture.height;
                _sourcePixels = readableTexture.GetPixels32();
                BuildFrameAlphaBounds();
            }
            catch (Exception exception)
            {
                _sourcePixels = null;
                _pixelLoadError = $"픽셀 데이터를 읽을 수 없습니다: {exception.Message}";
            }
            finally
            {
                if (readableTexture != null)
                    DestroyImmediate(readableTexture);
            }
        }

        void BuildFrameAlphaBounds()
        {
            EnsureFrameBuffers();
            if (_sourcePixels == null ||
                _sourceWidth != _columns * _cellSize ||
                _sourceHeight != _rows * _cellSize)
                return;

            for (int frameIndex = 0; frameIndex < FrameCount; frameIndex++)
            {
                int column = frameIndex % _columns;
                int rowFromTop = frameIndex / _columns;
                int cellOriginX = column * _cellSize;
                int cellOriginYBottom = _sourceHeight - ((rowFromTop + 1) * _cellSize);
                int minX = _cellSize;
                int minYTop = _cellSize;
                int maxX = -1;
                int maxYTop = -1;

                for (int localYBottom = 0; localYBottom < _cellSize; localYBottom++)
                {
                    int localYTop = _cellSize - 1 - localYBottom;
                    int sourceRow = (cellOriginYBottom + localYBottom) * _sourceWidth;
                    for (int localX = 0; localX < _cellSize; localX++)
                    {
                        Color32 pixel = _sourcePixels[sourceRow + cellOriginX + localX];
                        if (pixel.a <= VisibleAlphaThreshold)
                            continue;

                        minX = Mathf.Min(minX, localX);
                        minYTop = Mathf.Min(minYTop, localYTop);
                        maxX = Mathf.Max(maxX, localX);
                        maxYTop = Mathf.Max(maxYTop, localYTop);
                    }
                }

                _frameAlphaBounds[frameIndex] = maxX < minX
                    ? new RectInt(0, 0, 0, 0)
                    : new RectInt(minX, minYTop, maxX - minX + 1, maxYTop - minYTop + 1);
            }
        }

        int CountClippingFrames()
        {
            EnsureFrameBuffers();
            int clippingCount = 0;
            for (int frameIndex = 0; frameIndex < FrameCount; frameIndex++)
            {
                if (WouldFrameClip(frameIndex))
                    clippingCount++;
            }
            return clippingCount;
        }

        bool WouldFrameClip(int frameIndex)
        {
            if (_sourcePixels == null ||
                frameIndex < 0 ||
                frameIndex >= _frameAlphaBounds.Length)
                return false;

            RectInt bounds = _frameAlphaBounds[frameIndex];
            if (bounds.width <= 0 || bounds.height <= 0)
                return false;

            Vector2Int offset = GetFrameOffset(frameIndex);
            return bounds.xMin + offset.x < 0 ||
                   bounds.xMax + offset.x > _cellSize ||
                   bounds.yMin + offset.y < 0 ||
                   bounds.yMax + offset.y > _cellSize;
        }

        bool CanSaveAlignedPng()
        {
            return CanApplyStandards() &&
                   _sourcePixels != null &&
                   string.IsNullOrEmpty(_pixelLoadError) &&
                   HasPendingOffsets &&
                   CountClippingFrames() == 0;
        }

        void SaveAlignedPng()
        {
            if (!CanSaveAlignedPng())
                return;

            string assetPath = GetAssetPath();
            string offsetSummary = string.Join(
                ", ",
                _frameOffsets
                    .Select((offset, index) => (offset, index))
                    .Where(item => item.offset != Vector2Int.zero)
                    .Select(item => $"{item.index + 1}:({item.offset.x},{item.offset.y})"));
            bool confirmed = EditorUtility.DisplayDialog(
                "프레임 위치 보정 저장",
                $"{Path.GetFileName(assetPath)}의 PNG 픽셀을 셀 내부에서 이동합니다.\n\n" +
                $"변경 프레임: {offsetSummary}\n\n" +
                "저장 전 원본 PNG는 Library/CrossDefenseAnimationBackups에 백업됩니다.",
                "PNG 저장",
                "취소");
            if (!confirmed)
                return;

            string absolutePath = GetAbsoluteAssetPath(assetPath);
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                                 ?? throw new InvalidOperationException("Unity 프로젝트 루트를 찾을 수 없습니다.");
            string backupDirectory = Path.Combine(projectRoot, "Library", BackupFolderName);
            string backupPath = Path.Combine(
                backupDirectory,
                $"{Path.GetFileNameWithoutExtension(assetPath)}_{DateTime.Now:yyyyMMdd-HHmmssfff}.png");

            Texture2D outputTexture = null;
            try
            {
                Directory.CreateDirectory(backupDirectory);
                File.Copy(absolutePath, backupPath, false);

                Color32[] shiftedPixels = BuildShiftedPixels();
                outputTexture = new Texture2D(_sourceWidth, _sourceHeight, TextureFormat.RGBA32, false, false);
                outputTexture.SetPixels32(shiftedPixels);
                outputTexture.Apply(false, false);
                byte[] pngBytes = outputTexture.EncodeToPNG();
                if (pngBytes == null || pngBytes.Length == 0)
                    throw new InvalidOperationException("PNG 인코딩 결과가 비어 있습니다.");

                File.WriteAllBytes(absolutePath, pngBytes);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

                _sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                Array.Clear(_frameOffsets, 0, _frameOffsets.Length);
                LoadSourcePixels();
                Selection.activeObject = _sheet;
                _validationDirty = true;
                EnsureValidation();
                Debug.Log($"Animation sheet alignment saved: {assetPath}\nBackup: {backupPath}");
                ShowNotification(new GUIContent("위치 보정 PNG 저장 완료"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "PNG 저장 실패",
                    $"{exception.Message}\n\n백업 위치: {backupPath}",
                    "확인");
            }
            finally
            {
                if (outputTexture != null)
                    DestroyImmediate(outputTexture);
            }
        }

        Color32[] BuildShiftedPixels()
        {
            if (_sourcePixels == null)
                throw new InvalidOperationException("원본 픽셀 데이터가 없습니다.");

            var shiftedPixels = new Color32[_sourcePixels.Length];
            for (int frameIndex = 0; frameIndex < FrameCount; frameIndex++)
            {
                int column = frameIndex % _columns;
                int rowFromTop = frameIndex / _columns;
                int cellOriginX = column * _cellSize;
                int cellOriginYBottom = _sourceHeight - ((rowFromTop + 1) * _cellSize);
                Vector2Int offset = GetFrameOffset(frameIndex);

                for (int localYBottom = 0; localYBottom < _cellSize; localYBottom++)
                {
                    int targetYBottom = localYBottom - offset.y;
                    if (targetYBottom < 0 || targetYBottom >= _cellSize)
                        continue;

                    int sourceRow = (cellOriginYBottom + localYBottom) * _sourceWidth;
                    int targetRow = (cellOriginYBottom + targetYBottom) * _sourceWidth;
                    for (int localX = 0; localX < _cellSize; localX++)
                    {
                        int targetX = localX + offset.x;
                        if (targetX < 0 || targetX >= _cellSize)
                            continue;

                        shiftedPixels[targetRow + cellOriginX + targetX] =
                            _sourcePixels[sourceRow + cellOriginX + localX];
                    }
                }
            }

            return shiftedPixels;
        }

        static string GetAbsoluteAssetPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                                 ?? throw new InvalidOperationException("Unity 프로젝트 루트를 찾을 수 없습니다.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        SpriteRect[] BuildFixedGridRects(SpriteRect[] existingRects, string assetPath)
        {
            var available = existingRects.ToList();
            var repaired = new SpriteRect[_columns * _rows];
            string baseName = Path.GetFileNameWithoutExtension(assetPath);

            for (int index = 0; index < repaired.Length; index++)
            {
                Rect expectedRect = GetExpectedRect(index, _sheet.height);
                SpriteRect existing = TakeMatchingRect(available, index, expectedRect, true);
                repaired[index] = new SpriteRect
                {
                    name = existing?.name ?? $"{baseName}_{index}",
                    rect = expectedRect,
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = existing?.border ?? Vector4.zero,
                    customData = existing?.customData ?? string.Empty,
                    spriteID = existing?.spriteID ?? GUID.Generate()
                };
            }

            return repaired;
        }

        SpriteRect TakeMatchingRect(List<SpriteRect> available, int frameIndex, Rect expectedRect, bool allowNearest)
        {
            SpriteRect match = available.FirstOrDefault(rect => ParseFrameIndex(rect.name) == frameIndex);
            match ??= available.FirstOrDefault(rect => expectedRect.Contains(rect.rect.center));

            if (match == null && allowNearest && available.Count > 0)
                match = available.OrderBy(rect => Vector2.SqrMagnitude(rect.rect.center - expectedRect.center)).First();

            if (match != null)
                available.Remove(match);
            return match;
        }

        Rect GetExpectedRect(int frameIndex, int textureHeight)
        {
            int column = frameIndex % _columns;
            int rowFromTop = frameIndex / _columns;
            return new Rect(
                column * _cellSize,
                textureHeight - ((rowFromTop + 1) * _cellSize),
                _cellSize,
                _cellSize);
        }

        static ISpriteEditorDataProvider CreateDataProvider(TextureImporter importer)
        {
            if (importer == null)
                return null;

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider?.InitSpriteEditorDataProvider();
            return provider;
        }

        static int ParseFrameIndex(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return -1;

            int separatorIndex = spriteName.LastIndexOf('_');
            if (separatorIndex < 0 || separatorIndex == spriteName.Length - 1)
                return -1;

            return int.TryParse(spriteName[(separatorIndex + 1)..], out int index) ? index : -1;
        }

        static bool RectsApproximatelyEqual(Rect left, Rect right)
        {
            return Mathf.Approximately(left.x, right.x) &&
                   Mathf.Approximately(left.y, right.y) &&
                   Mathf.Approximately(left.width, right.width) &&
                   Mathf.Approximately(left.height, right.height);
        }

        static float InferExpectedPixelsPerUnit(string assetPath)
        {
            string normalizedPath = assetPath?.Replace('\\', '/').ToLowerInvariant() ?? string.Empty;
            if (normalizedPath.Contains("main_summoner"))
                return 140f;
            if (normalizedPath.Contains("enemy_goblin"))
                return 220f;
            return 200f;
        }

        static SpriteMeshType GetSpriteMeshType(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType;
        }

        static void SetSpriteMeshType(TextureImporter importer, SpriteMeshType meshType)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = meshType;
            importer.SetTextureSettings(settings);
        }

        string GetAssetPath()
        {
            return _sheet == null ? string.Empty : AssetDatabase.GetAssetPath(_sheet);
        }

        void AddIssue(MessageType type, string message)
        {
            _issues.Add(new ValidationIssue(type, message));
        }

        static GUIStyle CenteredLabelStyle()
        {
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            return style;
        }

        readonly struct ValidationIssue
        {
            public readonly MessageType Type;
            public readonly string Message;

            public ValidationIssue(MessageType type, string message)
            {
                Type = type;
                Message = message;
            }
        }
    }
}
