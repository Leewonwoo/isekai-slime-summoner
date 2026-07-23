# Animation Sheet Alignment Editor

캐릭터 3×3 애니메이션 시트의 슬라이스·피봇·임포트 설정을 검사하고, 셀 안의 실제 픽셀 위치를 프레임별로 보정하는 Unity Editor 전용 도구다. `ArtSource` 원본은 수정하지 않는다.

## 실행

1. Unity Project 창에서 `*_sheet.png`를 선택한다.
2. 메뉴 `Cross Defense > Animation Sheet Alignment`를 연다.
3. Validation과 Onion Skin 미리보기를 확인한다.
4. 메타데이터 교정이 필요하면 `고정 Grid + Import Settings 적용`을 누른다.
5. `Reference`에서 기준 프레임을 정하고, 고칠 프레임을 선택한다.
6. 방향 버튼 또는 방향키로 1px씩 이동한다. `Shift+방향키`는 4px 이동이다.
7. 튐이 사라지고 클리핑 경고가 없으면 `위치 보정 PNG 저장`을 누른다.

기본 프리셋은 3열×3행, 셀 128×128px, Unity 피봇 Center, 내용 루트 가이드 `(64, 112)`이다. PPU는 경로에 따라 소환사 140, 적 고블린 220, 그 외 캐릭터 200을 제안한다.

오프셋 좌표는 `X+ = 오른쪽`, `Y+ = 아래쪽`이다. 기준 프레임은 보통 중립 자세인 1번을 사용하고, 무기 끝이나 임시 공격부위가 아니라 몸체 밑면 중앙을 가이드에 맞춘다.

## 적용 범위

- Sprite Mode Multiple
- 9개 고정 128×128 Sprite Rect
- 모든 프레임 Center 피봇
- 캐릭터별 PPU
- Filter Point, Mipmap Off, Compression None, Mesh Full Rect
- 기존 Sprite ID를 이름과 셀 위치 기준으로 최대한 재사용
- 프레임별 1px/4px 픽셀 이동과 고정 기준 프레임 Onion Skin
- 셀 경계 클리핑 검사
- 저장 전 `Library/CrossDefenseAnimationBackups/`에 처리본 PNG 자동 백업

위치 보정 저장은 `Assets/Art`의 가공 완료 PNG만 덮어쓰고 기존 Sprite ID와 `ArtSource` 원본을 유지한다. `고정 Grid + Import Settings 적용`은 별도로 `.meta`의 슬라이스·피봇·임포트 설정을 교정한다. 자동 보정이 더 적합한 시트는 [normalize_animation_sheet.py](../tools/normalize_animation_sheet.py)를 먼저 사용한 뒤 이 도구로 미세 조정한다.
