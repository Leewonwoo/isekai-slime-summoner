# UI/UX 시스템 지침 — UI Toolkit (Cross Defense)

> 모든 UI 작업(UXML/USS/C# 컨트롤러)은 이 문서를 따른다. SPEC §3.2~3.3, §4의 구현 규격판.
> 여기 없는 값을 새로 정해야 하면 **이 문서에 먼저 추가하고** 코드를 쓴다. 문서에 없는 임의 값 사용 금지.

---

## 1. 역할 분리 (절대 규칙)

| 담당 | 그리는 것 |
|---|---|
| **UI Toolkit** | 상단 HUD, 하단 패널(탭/리스트/벤치), 방향 예고 배지, 스킬 플로팅 버튼, 소환사 스트립, 팝업 |
| **월드 (SpriteRenderer)** | 십자 필드, 유닛, 몹, 투사체, 코어, 이펙트 |

- 필드를 UI Toolkit으로 그리지 않는다. UI가 월드 위에 얹히는 건 **FieldOverlay의 배지/버튼**뿐.
- uGUI(Canvas) 사용 금지. UI는 전부 UI Toolkit으로 통일.

## 2. 파일 구조와 네이밍

```
Assets/UI/
  UXML/   RootLayout.uxml, TopHUD.uxml, FieldOverlay.uxml, BottomPanel.uxml,
          tabs/SummonTab.uxml … tabs/SummonerTab.uxml, templates/UpgradeRow.uxml
  USS/    variables.uss, common.uss, TopHUD.uss, FieldOverlay.uss, BottomPanel.uss, UpgradeRow.uss
  CrossDefensePanelSettings.asset
Assets/Scripts/UI/
  RootLayoutController.cs, TopHUDController.cs, FieldOverlayController.cs,
  BottomPanelController.cs, tabs/…, UpgradeRowView.cs
```

- **1 UXML = 1 USS = 1 C# 컨트롤러**, 세 파일 모두 같은 베이스 이름 (PascalCase).
- UXML의 UXML 구조는 SPEC §3.2 트리 고정. 새 화면이 필요하면 이 트리에 어디 붙는지부터 결정.
- **`name` 속성**: C#에서 조회할 요소에만 부여, kebab-case (`core-hp-bar`, `summon-button`).
- **USS 클래스**: kebab-case, `블록__요소--변형` (BEM 축약). 예: `badge`, `badge--danger`, `row__level-label`.

## 3. PanelSettings 표준 (CrossDefensePanelSettings.asset 하나만 사용)

| 항목 | 값 |
|---|---|
| Reference Resolution | **1080 × 1920** |
| Scale Mode | Scale With Screen Size |
| Screen Match Mode | Match Width Or Height, **Match = 0 (Width)** |
| Clear Color | 끔 (월드가 비쳐야 함) |

- 패널을 추가로 만들지 않는다. UIDocument는 RootLayout 하나에 모두 태운다.
- 세이프 에어리어: TopHUD와 SummonerStrip은 런타임에 `Screen.safeArea`를 읽어 상/하단 패딩을 C#에서 주입 (인라인 스타일 예외 허용 항목).

## 4. 디자인 토큰 — variables.uss (하드코딩 금지, 항상 var() 참조)

### 색상

```css
:root {
    /* 배경/표면 */
    --color-bg: rgb(18, 20, 28);            /* 앱 최심부 배경 */
    --color-panel: rgb(28, 32, 41);         /* 하단 패널 */
    --color-surface: rgb(38, 43, 56);       /* 카드/행/슬롯 */
    --color-border: rgb(58, 65, 82);

    /* 텍스트 */
    --color-text: rgb(242, 244, 248);
    --color-text-dim: rgb(168, 176, 192);
    --color-text-disabled: rgb(107, 114, 128);

    /* 위협도 (방향 예고 배지 — SPEC §4.2) */
    --color-danger: rgb(255, 82, 82);        /* 빨강 = 위험 */
    --color-warning: rgb(255, 193, 7);       /* 노랑 = 보통 */
    --color-muted: rgb(107, 114, 128);       /* 회색 = 없음 */

    /* 재화 */
    --color-gold: rgb(255, 192, 47);
    --color-gem: rgb(79, 195, 247);

    /* 등급 (SPEC §4.5: 일반 회색 → 영웅 보라 → 전설 금) */
    --color-rarity-normal: rgb(154, 163, 178);
    --color-rarity-hero: rgb(164, 108, 255);
    --color-rarity-legend: rgb(255, 179, 0);

    /* 상태 */
    --color-positive: rgb(76, 217, 100);     /* 합성 가능 테두리, 상승 수치 */
    --color-reddot: rgb(255, 59, 48);
}
```

### 간격 · 크기 · 라운드 (1080 기준 px)

```css
:root {
    --gap-xs: 4px;  --gap-sm: 8px;  --gap-md: 16px;  --gap-lg: 24px;  --gap-xl: 32px;
    --radius-sm: 8px;  --radius-md: 12px;  --radius-lg: 20px;
    --touch-min: 96px;         /* 터치 타깃 최소 한 변 */
    --row-height: 128px;       /* UpgradeRow 공용 행 높이 */
    --tab-height: 112px;       /* 탭바 높이 */
    --strip-height: 140px;     /* 소환사 스트립 높이 */
}
```

### 폰트 크기 (1080 기준)

| 토큰 | px | 용도 |
|---|---|---|
| `--font-display` | 56 | 웨이브 번호, 보스 경고 |
| `--font-title` | 44 | 탭 제목, 버튼 라벨(주요) |
| `--font-body` | 36 | 리스트 행, 수치 |
| `--font-caption` | 28 | 보조 설명, 확률 표기 |

- 폰트 에셋을 정하기 전까지 기본 폰트 사용. 폰트 추가 시 이 문서와 에셋 대장에 기록.

## 5. USS 작성 규칙

1. 색상·간격·크기는 **반드시 `var(--토큰)`**. 리터럴 색상이 USS에 보이면 리뷰 반려 대상.
2. 스타일은 USS에만. **C#에서 `style.*` 직접 조작 금지** — 상태 변화는 USS 클래스 토글(`AddToClassList`/`RemoveFromClassList`/`EnableInClassList`)로.
   - **예외 3가지만 허용**: ① 월드 앵커 위치 동기화(배지 `style.translate`), ② 세이프 에어리어 패딩, ③ 연속 수치 게이지 fill(`.gauge__fill`의 `style.width` — HP/EXP 등).
3. 상태 변형은 `--modifier` 클래스: `.badge--danger`, `.buy-button--disabled`, `.tab__button--active`, `.slot--mergeable`.
4. `:hover` 의존 금지 (모바일). 눌림 피드백은 `:active`로.
5. 전환 효과는 USS `transition`으로 선언 (예: 탭 콘텐츠 페이드, 패널 확장). DOTween은 월드 오브젝트 전용 — UI 트윈에 쓰지 않는다.
6. `common.uss`: 버튼/캡슐/레드닷 등 2곳 이상에서 쓰는 공용 클래스만. 한 화면 전용 스타일은 그 화면의 USS로.

## 6. UXML 규칙

1. 트리 구조는 SPEC §3.2에서 벗어나지 않는다. RootLayout이 TopHUD/FieldOverlay/BottomPanel을 `<ui:Instance>`(Template)로 포함.
2. 반복 요소(강화/스킬/소환사 행, 벤치 슬롯)는 **템플릿 1개 + 런타임 Instantiate**. 복붙으로 행을 늘리지 않는다.
3. UXML에 인라인 `style` 속성 금지 (레이아웃 프로토타입도 USS로).
4. FieldOverlay는 `position: absolute`, `picking-mode="Ignore"` 기본 — 배지/버튼 등 상호작용 요소만 개별적으로 피킹 허용. 필드 터치를 UI가 가로채면 안 된다.
5. 하단 패널 존 비율은 flex로: 상단 HUD ~8% / 필드 ~50% / 하단 패널 ~40% (SPEC §4.1). 픽셀 고정 높이는 행/탭/스트립 같은 내부 요소에만.

## 7. C# 컨트롤러 패턴

```csharp
// 모든 UI 컨트롤러의 공통 골격
public class TopHUDController
{
    readonly Label _waveLabel;          // 1) 생성자에서 UQuery 1회 캐싱
    public TopHUDController(VisualElement root)
    {
        _waveLabel = root.Q<Label>("wave-label");
    }
    public void Bind(GameState state) { /* 2) 이벤트 구독 */ }
    public void Unbind() { /* 3) 구독 해제 — Bind와 반드시 쌍 */ }
}
```

1. **MonoBehaviour는 UIDocument를 든 진입점(RootLayoutController) 하나만.** 나머지 컨트롤러는 plain C# 클래스로 root를 받아 생성.
2. `Q<T>()`는 생성자에서 1회만. Update 루프에서 UQuery 금지.
3. 게임 로직 → UI는 **이벤트/콜백 단방향**. UI 컨트롤러가 게임 상태를 직접 수정하지 않고 의도만 발행 (`OnSummonClicked` 등). 컨트롤러에 게임 규칙(가격 계산 등) 넣지 않는다.
4. 수치 표기는 항상 **"현재→다음"** 변화량 병기 (SPEC §4.5). 포맷 헬퍼는 `UIFormat` 정적 클래스 한 곳에.
5. 재화 부족 등 비활성 상태: `SetEnabled(false)` + `--disabled` 클래스. 버튼을 숨기지 않는다(회색 처리 원칙).
6. 길게 누르기(연속 강화): PointerDown 후 0.4s 지연 → 0.08s 간격 반복. 공용 헬퍼 `LongPressRepeater`로 구현해 재사용.

## 8. 월드 ↔ UI 브릿지 (스파이크 A/B의 규격)

### 8.1 월드 앵커 배지 (방향 예고 배지 4개)
- FieldOverlay의 배지 요소를 매 프레임(또는 카메라/레이아웃 변경 시) 동기화:
  `Camera.WorldToScreenPoint(월드앵커)` → `RuntimePanelUtils.ScreenToPanel(panel, screenPos)` → `style.translate`.
- Y축 반전 주의: 스크린 좌표는 하단 원점, 패널 좌표는 상단 원점.
- 이 동기화 코드는 `WorldAnchorBinder` 하나로 일반화해 배지 4개 + 이후 다른 월드 앵커 UI에 재사용.

### 8.2 벤치(UI) → 필드(월드) 드래그 앤 드롭
- 흐름: 벤치 슬롯 `PointerDownEvent` → 루트에 고스트 요소 생성(`picking-mode: Ignore`) → `PointerMoveEvent`로 고스트 이동 + 포인터 캡처 → `PointerUpEvent`에서 스크린 좌표로 `Physics2D.OverlapPoint`(슬롯 레이어) → 유효 슬롯이면 스냅 배치, 아니면 벤치 복귀.
- 드래그 중 필드 슬롯 하이라이트는 월드 쪽(슬롯 SpriteRenderer 틴트)에서 처리.

## 9. 인터랙션 필수 문법 (키우기 문법 — SPEC §4)

- **방향 예고 배지**: "N ×24" 형식, 위협도 색(`--color-danger/warning/muted`). 하단 패널에 절대 묻히지 않게 FieldOverlay 소속.
- **레드닷**: 탭 버튼 우상단 12px 원, `--color-reddot`. 강화 가능/새 장비 시 표시. 공용 클래스 `.reddot`.
- **탭 전환**: 탭 열면 하단 패널 확장 + 필드 축소(USS transition), 전투는 계속 보이게.
- **소환 버튼**: 우측 엄지 위치(패널 우하단), 비용 표시, 등급 확률 노출.
- **합성 가능 벤치 슬롯**: `--color-positive` 테두리 + "합성" 뱃지.
- **터치 타깃**: 상호작용 요소는 최소 `--touch-min`(96px) 확보.

## 10. Do / Don't 요약

| ✅ Do | ❌ Don't |
|---|---|
| 색상/간격은 `var(--토큰)` | USS/UXML/C#에 리터럴 색상·매직넘버 |
| 상태 변화 = USS 클래스 토글 | C# `style.*` 직접 조작 (예외 2가지 외) |
| 행/슬롯은 템플릿 + 런타임 생성 | UXML에 행 복붙 |
| 이벤트 단방향 (UI는 의도만 발행) | UI 컨트롤러에서 게임 로직 수정 |
| 비활성 = 회색 + SetEnabled(false) | 버튼 숨기기 |
| PanelSettings 1개, UIDocument 1개 | 화면별 패널 남발 |
| UI 전환은 USS transition | UI에 DOTween 사용 |
