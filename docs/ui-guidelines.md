# UI/UX 시스템 지침 — UI Toolkit (Cross Defense)

> 모든 UI 작업(UXML/USS/C# 컨트롤러)은 이 문서를 따른다. SPEC §3.2~3.3, §4의 구현 규격판.
> 여기 없는 값을 새로 정해야 하면 **이 문서에 먼저 추가하고** 코드를 쓴다. 문서에 없는 임의 값 사용 금지.

---

## 1. 역할 분리 (절대 규칙)

| 담당 | 그리는 것 |
|---|---|
| **UI Toolkit** | 상단 HUD, 하단 패널(탭/리스트/벤치/소환사 프로필), 필드 상단 웨이브 상태 표시, 스킬 플로팅 버튼, 팝업 |
| **월드 (SpriteRenderer)** | 자연 숲 필드, 유닛, 몹, 투사체, 소환사·소환진 데칼(=코어), 개체 체력바, 이펙트 |

- 필드를 UI Toolkit으로 그리지 않는다. UI가 월드 위에 얹히는 건 **FieldOverlay의 배지/버튼**뿐.
- 소환사·소환수·몬스터 체력바는 개체와 함께 풀링되는 월드 `SpriteRenderer`로 그린다. 개체별 Canvas/UIDocument를 추가하지 않고, HP 변경 이벤트 때만 fill 크기를 갱신한다.
- uGUI(Canvas) 사용 금지. UI는 전부 UI Toolkit으로 통일.

### 영구·런 특성 3택 uGUI 예외 (2026-07-17 사용자 지시)

- 소환사 레벨업 영구 특성과 5웨이브 런 특성 3택용 `ChoicePrototypeCanvas`만 uGUI로 사용한다.
- 기준 해상도는 **1080×1920**, CanvasScaler는 Scale With Screen Size / Match Width Or Height **0**을 사용한다.
- 팝업은 `Assets/Art/UIFrames/choice_modal_frame.png`를 9-slice로 사용하며 기준 크기 **920×1450px**, Sprite Border **64px**로 설정한다.
- 영구 특성은 제목 `영구 특성 선택`, 부제 `레벨업 보상 · 남은 선택 N`으로 표시한다.
- 런 특성은 제목 `런 특성 선택`, 부제 `WAVE N 클리어 · 사망 시 소멸`로 표시한다.
- 두 팝업 모두 카드에 이름·이번 선택의 증가량·선택 후 누적 레벨을 표시하고 첫 번째 카드를 기본 선택한다.
- 팝업은 미선택 특성이 있을 때만 생성·표시하고 선택 완료 후 닫는다. 미선택 수가 2개 이상이면 다음 프레임에 다음 3택을 연속 표시한다.
- 영구 선택권과 런 선택이 동시에 대기하면 현재 팝업을 먼저 마친 뒤 런 선택을 우선 표시해 웨이브 진행 정지를 해제한다.
- 이 팝업 외의 화면으로 uGUI 사용 범위를 확대하지 않는다.

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
- 세이프 에어리어: RootLayout은 런타임에 `Screen.safeArea`를 읽어 상/하단 패딩을 C#에서 주입한다 (인라인 스타일 예외 허용 항목).

## 4. 디자인 토큰 — variables.uss (하드코딩 금지, 항상 var() 참조)

### 색상 — "척박한 숲" 픽셀 스킨 (돌 프레임 × 나무 판재, 2026-07-11 v2)

> 컨셉: 척박한 숲 개간지(픽셀 아트, SPEC §5.1 v2)와 한 몸으로 보이는 UI. 프레임은 바랜 돌, 조작면(버튼/행)은 나무 판재 + 적갈 아웃라인, 판재 위 텍스트는 잉크색(진갈). 골드는 플레이어의 "행동"(주요 버튼·재화)에만 쓰고, 위협도 색과 절대 섞지 않는다.
> 최종형은 9-slice 픽셀 프레임 스프라이트(아래 "픽셀 프레임" 절) — 스프라이트 생성 전까지는 아래 플랫 색상이 근사 플레이스홀더.

```css
:root {
    /* 배경/표면 — 바랜 돌 + 나무 판재 */
    --color-bg: rgb(24, 21, 15);            /* 앱 최심부 배경(숲 심야 흙) #18150F */
    --color-transparent: rgba(0, 0, 0, 0);  /* 투명 레이어 */
    --color-panel: rgb(66, 63, 53);         /* 하단 패널/프레임(바랜 돌) #423F35 */
    --color-panel-deep: rgb(43, 41, 34);    /* 돌 음영·움푹한 곳(벤치 슬롯 바닥) #2B2922 */
    --color-surface: rgb(198, 166, 106);    /* 조작면(나무 판재 — 버튼/행) #C6A66A */
    --color-surface-deep: rgb(151, 118, 62);/* 판재 하단 엣지 #97763E */
    --color-border: rgb(112, 58, 40);       /* 적갈 아웃라인 #703A28 */
    --color-modal-scrim: rgba(0, 0, 0, 0.72);

    /* 텍스트 — 어두운 바탕엔 웜 화이트, 판재 위엔 잉크 */
    --color-text: rgb(240, 234, 214);       /* #F0EAD6 */
    --color-text-dim: rgb(178, 170, 144);   /* #B2AA90 */
    --color-text-disabled: rgb(124, 118, 99);
    --color-ink: rgb(59, 42, 23);           /* 판재 위 본문 #3B2A17 */
    --color-ink-dim: rgb(110, 86, 50);      /* 판재 위 보조 #6E5632 */

    /* 위협도 (필드/전투 상태 확장용) */
    --color-danger: rgb(255, 82, 82);        /* 빨강 = 위험 */
    --color-warning: rgb(255, 193, 7);       /* 노랑 = 보통 */
    --color-muted: rgb(107, 114, 128);       /* 회색 = 없음 */

    /* 재화/액션 */
    --color-gold: rgb(255, 192, 47);
    --color-gold-deep: rgb(184, 128, 26);    /* 골드 버튼 보더/하단 엣지 #B8801A */
    --color-gem: rgb(79, 195, 247);

    /* 등급 (SPEC §4.5: 일반 회색 → 영웅 보라 → 전설 금) */
    --color-rarity-normal: rgb(154, 163, 178);
    --color-rarity-hero: rgb(164, 108, 255);
    --color-rarity-legend: rgb(255, 179, 0);

    /* 상태 */
    --color-positive: rgb(76, 217, 100);     /* 합성 가능 테두리, 상승 수치 */
    --color-reddot: rgb(255, 59, 48);

    /* 속성 미니 아이콘 (SPEC §2.8 — 배지 안 표시 전용, 위협도 색 채널과 분리) */
    --color-attr-none: rgb(154, 163, 178);   /* 무 */
    --color-attr-fire: rgb(255, 112, 67);    /* 화염 */
    --color-attr-ice: rgb(79, 195, 247);     /* 빙결 */
    --color-attr-nature: rgb(129, 199, 132); /* 자연 */

    /* TopHUD 구조 (1080 기준) */
    --top-hud-profile-width: 264px;
    --top-hud-stage-width: 180px;
    --wave-status-width: 200px;
    --top-hud-settings-width: 104px;
    --top-hud-settings-icon-size: 64px;
    --currency-icon-size: 44px;
    --top-hud-portrait-size: 96px;
    --top-hud-portrait-radius: 48px;
    --summoner-portrait-size: 104px;
    --summoner-portrait-radius: 52px;
    --portrait-ring-inset: 12px;
    --skill-button-size: 128px;
    --skill-button-radius: 64px;
    --bottom-panel-content-inset: 40px;
    --bench-slot-state-border: 4px;
    --bench-slot-size: 120px;               /* 장비 슬롯 등 공용 기본값 */
    --summon-grid-slot-width: 188px;
    --summon-grid-slot-height: 210px;
    --summon-grid-icon-size: 112px;
    --summon-merge-badge-height: 36px;
    --summon-scrollbar-width: 16px;
    --summon-side-width: 320px;
    --summon-contract-panel-height: 96px;
    --unit-detail-width: 760px;
    --unit-detail-height: 920px;
    --unit-detail-icon-size: 200px;
    --unit-detail-stat-row-height: 72px;
    --unit-detail-content-inset: 72px;
    --font-small: 20px;
}
```

### 스킨 문법 (모든 컴포넌트 공통)

1. **청키 버튼**: `.btn`은 나무 판재(`--color-surface`) + 적갈 아웃라인(`--color-border`) + 두꺼운 하단 엣지(6px, `--color-surface-deep`). 판재 위 텍스트는 `--color-ink`. `:active`에서 하단 보더 2px + `translate: 0 4px`(눌림 피드백). 골드 버튼(주요 행동)만 `--color-gold`/`--color-gold-deep`.
2. **상태 배지**: 전투 상태 배지가 추가될 경우 위협도 색을 **배경**으로 채우고 `--color-bg` 아웃라인 — 필드 위 즉독성. warning(노랑) 배경 위 텍스트는 `--color-bg`. 속성은 `--color-attr-*` 미니 아이콘으로만.
3. **골드 = 행동**: 골드는 주요 버튼·재화 표기에만. 위협도(빨/노/회)·합성(초록)과 역할이 겹치지 않게.
4. **탭바**: 바탕 `--color-bg`, 활성 탭 = 나무 판재(`--color-surface`) + `--color-ink` 텍스트 + 상단 라운드 — "눌려서 켜진 나무 버튼"으로 읽히게. 비활성 탭은 돌 톤(`--color-text-dim`).

### 픽셀 프레임 (9-slice — 프레임 스프라이트 생성 후 적용)

- 대상: 하단 패널 외곽(돌 프레임), `.btn`(나무 버튼), `.row`(나무 판), 벤치 슬롯(돌 홈). 프롬프트는 SPEC §5.1 레이어 2 "UI 프레임".
- 적용법: `background-image` + `-unity-slice-left/-top/-right/-bottom`(px) + `-unity-slice-scale`. 슬라이스 값은 프레임 두께와 일치시키고 이 문서에 기록.
- TopHUD 프레임: `Assets/Art/UIFrames/top_hud_frame.png` — slice left/right **96px**, top/bottom **72px**, scale **0.4**. 원본은 `ArtSource/ui-frames/top_hud_frame.png`.
- TopHUD 프레임 배경은 UI 요소 전체를 채우도록 `background-size: 100% 100%`와 `background-repeat: no-repeat`을 사용한다.
- TopHUD 설정 버튼 아이콘: `Assets/Art/UIIcons/icon_settings.png` — **64×64px**로 중앙 정렬하고 버튼 텍스트는 표시하지 않는다. 터치 영역은 유지하되 버튼 배경과 테두리는 투명하게 표시한다. 원본은 `ArtSource/ui-icons/icon_settings.png`.
- BottomPanel 콘텐츠 프레임: `Assets/Art/UIFrames/bottom_panel_content_frame.png` — `.tab-content`에 적용, slice left/right/top/bottom **92px**, scale **0.4**, 내부 안전 여백 **40px**. 원본은 `ArtSource/ui-frames/bottom_panel_content_frame.png`.
- 벤치·장비 공용 슬롯 프레임: `Assets/Art/UIFrames/bench_slot_frame.png` — `.bench-slot`에 적용, slice left/right/top/bottom **190px**, scale **0.1**. 기본 상태 테두리는 투명, 합성 가능 상태는 **4px** 초록 CSS 테두리를 이미지 위에 표시한다. 원본은 `ArtSource/ui-frames/bench_slot_frame.png`.
- 공용 버튼 프레임: `Assets/Art/UIFrames/button_wood_frame.png`은 `.btn`, `button_gold_frame.png`은 `.btn--primary`에 적용한다. 두 이미지 모두 slice left/right **180px**, top/bottom **160px**, scale **0.1**. 비활성 `.btn--disabled`는 배경 이미지를 제거하고 기존 회색 플랫 스타일을 사용한다. 원본은 `ArtSource/ui-frames/`에 보존한다.
- 원형 링 프레임: `Assets/Art/UIFrames/frame_ring.png` — **256×256px** 고정형(9-slice 미사용). 스킬 플로팅 버튼 128px, TopHUD 소환사 초상화 96px, SummonerTab 초상화 104px에 공용 적용하고 초상화 이미지는 링 안쪽 자식 요소에 배치한다. 원본은 `ArtSource/ui-frames/frame_ring.png`.
- 하단 탭 프레임: `Assets/Art/UIFrames/tab_button_inactive_frame.png`과 `tab_button_active_frame.png` — 모든 탭 버튼에 9-slice로 적용한다. slice left/right **56px**, top/bottom **48px**, scale **0.35**. 활성 탭은 밝은 목재와 상단 골드 라인, 비활성 탭은 어두운 목재 홈으로 구분한다. 원본은 `ArtSource/ui-frames/`에 보존한다.
- 3택 프로토타입 팝업 프레임: `Assets/Art/UIFrames/choice_modal_frame.png` — uGUI `Image.Type.Sliced`, Sprite Border left/top/right/bottom **64px**, 기준 표시 크기 **920×1450px**. 원본은 `ArtSource/ui-frames/choice_modal_frame.png`.
- 소환 룰렛 모달 외곽: `Assets/Art/UIFrames/summon_roulette_panel_frame.png` — `.summon-modal__panel`에 9-slice로 적용한다. 원본 크기 **768×1024px**, slice left/top/right/bottom **112px**, scale **0.45**, 내부 안전 여백 **72px**. 원본은 `ArtSource/ui-frames/summon_roulette_panel_frame.png`.
- 소환 룰렛 릴 창: `Assets/Art/UIFrames/summon_roulette_reel_frame.png` — `.summon-modal-reel-viewport`에 적용한다. 원본 크기 **1024×288px**, 기준 표시 비율 **32:9**, `background-size: 100% 100%`, 좌우 내부 여백 **48px**. 중앙 상·하 포인터가 늘어나므로 9-slice는 사용하지 않는다. 원본은 `ArtSource/ui-frames/summon_roulette_reel_frame.png`.
- 소환 룰렛 카드: 기본 상태는 기존 `bench_slot_frame.png`, 최종 결과는 기존 `button_gold_frame.png`를 재사용한다. 카드 기준 크기는 **180×150px**, 카드 간격은 **0px**로 서로 맞붙여 하나의 연속 릴처럼 보이게 하며 별도 단색 배경과 CSS 테두리는 두지 않는다. 확정 결과 앞뒤에 최소 3개 이상의 미끼 카드를 유지하고 결과 카드는 중앙 포인터에서 정지해야 한다.
- 스프라이트 임포트: Filter **Point**, Compression **None** (SPEC §5.1 픽셀 임포트 규격).
- 9-slice 적용 후에도 위 플랫 토큰은 폴백·틴트 기준으로 유지 (프레임 없는 요소·게이지 등).
- 폰트: 픽셀 스킨 확정에 따라 한글 픽셀 폰트(갈무리 Galmuri 또는 네오둥근모, 둘 다 OFL) 도입 검토 — 도입 시 이 표와 에셋 대장에 기록 후 교체.

### 간격 · 크기 · 라운드 (1080 기준 px)

```css
:root {
    --gap-xs: 4px;  --gap-sm: 8px;  --gap-md: 16px;  --gap-lg: 24px;  --gap-xl: 32px;
    --radius-sm: 8px;  --radius-md: 12px;  --radius-lg: 20px;
    --touch-min: 96px;         /* 터치 타깃 최소 한 변 */
    --row-height: 128px;       /* UpgradeRow 공용 행 높이 */
    --row-icon-size: 88px;     /* UpgradeRow 스탯 아이콘 영역 */
    --tab-height: 112px;       /* 탭바 높이 */
    --tab-button-width: 20%;   /* 하단 고정 5탭 균등 폭 */
    --tab-icon-size: 44px;     /* 하단 5탭 아이콘 */
    --tab-frame-slice-x: 56px;
    --tab-frame-slice-y: 48px;
    --tab-frame-slice-scale: 0.35;
    --summoner-tab-profile-height: 140px; /* 소환사 탭 상단 프로필 높이 */
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
5. 전환 효과는 USS `transition`으로 선언 (예: 탭 콘텐츠 페이드, 패널 확장). DOTween은 원칙적으로 월드 오브젝트 전용이다. 단, 소환 룰렛 릴과 몬스터 드랍 골드가 HUD에 도착한 직후의 **골드 숫자 카운트업**은 값 보간만 허용하며 레이아웃·색상·위치를 직접 트윈하지 않는다.
6. `common.uss`: 버튼/캡슐/레드닷 등 2곳 이상에서 쓰는 공용 클래스만. 한 화면 전용 스타일은 그 화면의 USS로.

## 6. UXML 규칙

1. 트리 구조는 SPEC §3.2에서 벗어나지 않는다. RootLayout이 TopHUD/FieldOverlay/BottomPanel을 `<ui:Instance>`(Template)로 포함.
2. 반복 요소(강화/스킬/소환사 행, 벤치 슬롯)는 **템플릿 1개 + 런타임 Instantiate**. 복붙으로 행을 늘리지 않는다.
3. UXML에 인라인 `style` 속성 금지 (레이아웃 프로토타입도 USS로).
4. FieldOverlay는 `position: absolute`, `picking-mode="Ignore"` 기본 — 스킬 버튼 등 상호작용 요소만 개별적으로 피킹 허용. 필드 터치를 UI가 가로채면 안 된다.
5. 하단 패널 존 비율은 flex로: 상단 HUD ~8% / 필드 ~50% / 하단 패널 ~40% (SPEC §4.1). 픽셀 고정 높이는 행/탭/소환사 탭 프로필 같은 내부 요소에만.

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

### 8.1 필드 상단 웨이브 상태
- 현재 웨이브와 잔여 몬스터 수는 TopHUD 아래의 FieldOverlay 상단 중앙에 표시한다.
- 방향 예고 배지(N/E/S/W)는 사용하지 않는다. FieldOverlay는 웨이브 상태와 스킬 플로팅 버튼을 소유한다.

### 8.2 벤치(UI) → 필드(월드) 드래그 앤 드롭
- 흐름: 벤치 슬롯 `PointerDownEvent` → 루트에 고스트 요소 생성(`picking-mode: Ignore`) → `PointerMoveEvent`로 고스트 이동 + 포인터 캡처 → `PointerUpEvent`에서 스크린 좌표로 `Physics2D.OverlapPoint`(슬롯 레이어) → 유효 슬롯이면 스냅 배치, 아니면 벤치 복귀.
- 드래그 중 필드 슬롯 하이라이트는 월드 쪽(슬롯 SpriteRenderer 틴트)에서 처리.

## 9. 인터랙션 필수 문법 (키우기 문법 — SPEC §4)

- **웨이브 상태**: HUD 아래 필드 상단 중앙에 현재 웨이브(예: `WAVE 12`)와 잔여 몬스터 수를 함께 표시한다. 전체 웨이브 한계는 표시하지 않는다.
- **레드닷**: 탭 버튼 우상단 12px 원, `--color-reddot`. 강화 가능/새 장비 시 표시. 공용 클래스 `.reddot`.
- **탭 전환**: 탭 열면 하단 패널 확장 + 필드 축소(USS transition), 전투는 계속 보이게.
- **소환 탭**: 좌측은 벤치 대기 + 필드 배치 개체를 합친 전체 보유 유닛을 12슬롯 **3열 × N 세로 스크롤 그리드**로 배치한다. 같은 유닛 + 같은 머지 등급은 슬롯 하나로 묶고 좌상단에 전체 `xN`, 본문에 유닛 공통 `Lv.N`과 머지 등급을 표시한다. 헤더는 `보유 슬롯 n/12 · 총 m개` 형식이며 슬롯 한도는 개체 수가 아니라 스택 수를 센다. 수량 3개 이상인 ★3 미만 스택은 합성 가능 상태로 표시한다. 우측은 `용병 계약서 n장`·★1 직행 확률·`소환 ×1` 버튼을 세로 배치한다. 해금된 8종 전체를 선택 슬롯처럼 상시 나열하지 않는다.
- **소환 슬롯 탭/드래그 분리**: 포인터 이동이 드래그 임계값 미만이면 유닛 상세 팝업을 연다. 임계값 이상일 때 해당 스택에 벤치 대기 개체가 있으면 그 인스턴스 1개로 기존 벤치→필드 배치를 시작하고, 필드 배치 개체만 있으면 드래그를 시작하지 않는다. 상세 팝업은 전체 스택 수량·공통 강화 레벨·머지 등급·속성·공격 방식과 강화 배율까지 반영한 현재 전투 수치를 표시하며 열려 있는 동안 필드 클릭 공격을 차단한다.
- **소환 버튼**: 우측 엄지 위치(패널 우하단), 용병 계약서 1장 비용과 등급 확률을 노출한다. 계약서가 0장이면 회색 비활성화한다.
- **합성 가능 벤치 슬롯**: `--color-positive` 테두리 + "합성" 뱃지.
- **강화 탭**: 상단부터 전체 공격력·전체 공격속도·소환사 HP 회복·치명타 런 강화 4행을 표시하고, 그 아래에 현재 보유한 슬라임 종류별 공통 레벨 행을 표시한다. 각 행은 현재→다음 수치와 골드 비용을 함께 표시하며, 골드 부족·최대 레벨·HP 회복이 불필요한 상태에서는 버튼을 회색 비활성화한다. 강화 버튼은 짧게 누르면 1회, 0.4초 이상 누르면 0.08초 간격으로 반복 시도한다.
- **소환사 영구 성장**: 소환사 탭 프로필은 저장된 영구 Lv와 현재 EXP/요구 EXP 게이지를 표시한다. EXP가 충분할 때만 `레벨업` 버튼을 활성화하고, 공격력·최대 HP·★1 직행 확률의 현재→다음 영구 수치를 공용 행으로 표시한다.
- **터치 타깃**: 상호작용 요소는 최소 `--touch-min`(96px) 확보.

## 10. Do / Don't 요약

### 슬롯 릴 애니메이션 예외

- 소환 룰렛 릴은 제한된 UI 애니메이션 예외로, `SummonRouletteView`가 런타임 생성 `summon-reel-strip`의 `style.translate`와 결과 카드의 `style.scale`을 DOTween `DOTween.To`로 보간할 수 있다. 결과 카드는 배열 끝이 아닌 중간에 배치하고, 스킵 여부와 관계없이 중앙 포인터에 동일하게 정렬한다.
- 릴은 기존 `BottomPanel` 내부에만 두며 별도 UIDocument나 Canvas를 만들지 않는다. 결과는 애니메이션 시작 전에 게임 로직이 확정한다.

| ✅ Do | ❌ Don't |
|---|---|
| 색상/간격은 `var(--토큰)` | USS/UXML/C#에 리터럴 색상·매직넘버 |
| 상태 변화 = USS 클래스 토글 | C# `style.*` 직접 조작 (예외 2가지 외) |
| 행/슬롯은 템플릿 + 런타임 생성 | UXML에 행 복붙 |
| 이벤트 단방향 (UI는 의도만 발행) | UI 컨트롤러에서 게임 로직 수정 |
| 비활성 = 회색 + SetEnabled(false) | 버튼 숨기기 |
| PanelSettings 1개, UIDocument 1개 | 화면별 패널 남발 |
| UI 전환은 USS transition | UI에 DOTween 사용 |

## 11. 재화 아이콘

- TopHUD 재화 캡슐은 `Assets/Art/UIIcons/icon_gold.png`와 `Assets/Art/UIIcons/icon_gem.png`을 사용한다.
- `capsule__icon`은 `--currency-icon-size`로 크기를 제어하며, 아이콘 PNG의 투명 배경을 그대로 표시한다.
- 용병 계약서는 소환 탭 전용 런 재화이므로 TopHUD에 추가하지 않는다. 계약서 아이콘 에셋 제작 전에는 소환 탭의 텍스트 캡슐로 보유량을 표시한다.
- 강화 스탯 아이콘은 `Assets/Art/UIIcons/icon_atk.png`, `icon_aspd.png`, `icon_crit.png`, `icon_hp.png`, `icon_range.png`, `icon_income.png`을 사용한다. 모두 **128×128px** 투명 PNG이며 UpgradeRow의 88px 아이콘 영역에 `background-size: 100% 100%`로 표시한다.
- 하단 5개 탭은 `icon_tab_summon.png`, `icon_tab_upgrade.png`, `icon_tab_skill.png`, `icon_tab_gear.png`, `icon_tab_summoner.png`을 사용한다. 모두 **128×128px** 투명 PNG이며 **44px**로 표시하고, 탭 텍스트는 기능명 확인을 위해 유지한다.
