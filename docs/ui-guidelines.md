# UI/UX 시스템 지침 — UI Toolkit (Isekai Slime Summoner)

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

### 영구 특성·5웨이브 보상 3택 uGUI 예외 (2026-07-17, 2026-07-18 사용자 지시)

- 소환사 레벨업 영구 특성과 5웨이브 런 보상 3택용 `ChoicePrototypeCanvas`만 uGUI로 사용한다.
- 기준 해상도는 **1080×1920**, CanvasScaler는 Scale With Screen Size / Match Width Or Height **0**을 사용한다.
- 팝업은 `Assets/Art/UIFrames/choice_modal_frame.png`를 9-slice로 사용하며 기준 크기 **920×1450px**, Sprite Border **64px**로 설정한다.
- 영구 특성은 제목 `영구 특성 선택`, 부제 `레벨업 보상 · 남은 선택 N`으로 표시한다.
- 첫 5웨이브 런 보상은 제목 `소환사 속성 각성`, 부제 `WAVE N 클리어 · 이번 런의 주 공격 선택`으로 표시한다.
- 이후 런 보상은 제목 `웨이브 보상 선택`, 부제 `WAVE N 클리어 · 사망 시 소멸`로 표시한다.
- 런 보상 카드에는 카테고리(`소환사 각성`/`공격 진화`/`슬라임 군단`/`운명의 소환`), 이름, `NEW`·`Lv.N → Lv.N+1`·`즉시 보상` 상태와 실제 행동 변화를 표시한다. 첫 번째 카드를 기본 선택한다.
- 첫 런 보상은 화염구·빙결창·뇌격참 3장을 고정 제시한다. 이후에는 현재 공격에 맞는 진화 1장, 슬라임 군단 1장, 즉시 소환 1장을 기본 슬롯 구성으로 사용한다.
- 운명의 소환 카드를 선택하면 특성 팝업을 닫은 뒤 기존 전체화면 소환 룰렛을 계약서 소모·재화 꽝 없이 연속 재생한다. 모든 결과 연출이 끝날 때까지 게임플레이 일시정지를 유지한다.
- 팝업은 미선택 특성이 있을 때만 생성·표시하고 선택 완료 후 닫는다. 미선택 수가 2개 이상이면 다음 프레임에 다음 3택을 연속 표시한다.
- 영구 선택권과 런 선택이 동시에 대기하면 현재 팝업을 먼저 마친 뒤 런 선택을 우선 표시해 웨이브 진행 정지를 해제한다.
- 영구·런 특성 팝업을 표시하는 순간 게임플레이 일시정지 사유를 획득하고, 연속 선택권이 모두 소진된 뒤 해제한다. 팝업 버튼 입력은 `Time.timeScale == 0`에서도 동작해야 한다.
- 이 팝업 외의 화면으로 uGUI 사용 범위를 확대하지 않는다.

### 데미지 플로팅 텍스트 uGUI 예외 (2026-07-18 사용자 지시)

- 전투 데미지 숫자는 런타임에 생성하는 단일 `DamageFloatingTextCanvas`에서만 uGUI로 표시한다. 액터별 Canvas는 만들지 않는다.
- Canvas는 Screen Space Overlay, 기준 해상도 **1080×1920**, CanvasScaler는 Scale With Screen Size / Match Width Or Height **0**, Sorting Order **1000**을 사용한다. 특성 팝업의 Sorting Order 5000보다 아래에 둔다.
- 전용 Canvas에는 `GraphicRaycaster`를 붙이지 않는다. 데미지 `Text`는 모두 `raycastTarget = false`, `maskable = false`, `CanvasRenderer.cullTransparentMesh = true`로 설정한다.
- 숫자는 기본 uGUI 폰트와 같은 내장 Arial 폰트·기본 머티리얼을 모든 개체가 공유한다. 기준 글자 크기는 **44px**, 고정 RectTransform 크기는 **240×90px**이며 Best Fit·리치 텍스트·Layout 계열 컴포넌트는 사용하지 않는다.
- 플레이어가 적에게 준 피해는 `--color-text`와 같은 **RGB(240, 234, 214)**, 고블린이 슬라임·소환사에게 준 피해는 `--color-danger`와 같은 **RGB(255, 82, 82)**, 버프 슬라임 회복은 `--color-positive`와 같은 **RGB(76, 217, 100)** 및 `+N` 형식을 사용한다. 가독성용 uGUI `Shadow`는 검정 72%·오프셋 **(2, -2)px** 1개만 허용하며 별도 머티리얼을 만들지 않는다.
- 풀은 **24개 선생성**, **최대 64개**로 제한한다. 상한에 도달하면 수명이 가장 많이 지난 항목을 재사용하며 전투 중 Instantiate/Destroy 폭증을 막는다.
- 수명은 **0.72초**, 상승 거리는 **84px**, 시작 가로 분산은 **±14px**로 고정한다. 별도 코루틴·Animator·DOTween 없이 서비스 1개의 `LateUpdate`에서 활성 항목을 일괄 갱신한다.
- 전용 Canvas 분리는 움직이는 숫자의 Canvas 리빌드가 정적 HUD·하단 패널·특성 팝업으로 전파되지 않게 하기 위한 것이다. 같은 폰트·머티리얼과 연속된 계층을 유지해 활성 숫자가 배칭될 수 있게 한다.
- 데미지 숫자는 `Time.deltaTime`을 사용해 소환 룰렛·특성 팝업의 게임플레이 일시정지 중에는 전투와 함께 멈춘다.

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
    --color-overdrive: rgb(207, 105, 255);   /* 마력 폭주 게이지·활성 상태 */
    --color-overdrive-deep: rgb(102, 43, 128);

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
    --skill-button-icon-size: 56px;
    --skill-button-label-size: 24px;
    --skill-button-cooldown-size: 30px;
    --buff-skill-button-size: 96px;
    --buff-skill-button-radius: 48px;
    --buff-skill-icon-size: 40px;
    --buff-skill-cluster-right: 24px;
    --buff-skill-cluster-bottom: 168px;
    --codex-button-size: 96px;
    --codex-button-width: 200px;
    --codex-slot-size: 188px;
    --codex-slot-image-size: 72%;
    --codex-detail-height: 280px;
    --codex-detail-image-size: 220px;
    --unlock-toast-inset-x: 25%;
    --unlock-toast-top: 24%;
    --modal-panel-width: 920px;
    --modal-panel-height: 1450px;
    --merchant-card-height: 300px;
    --overdrive-status-width: 240px;
    --overdrive-status-height: 32px;
    --overdrive-gauge-width: 48px;
    --overdrive-gauge-height: 128px;
    --overdrive-gauge-right: 168px;
    --overdrive-gauge-bottom: 24px;
    --overdrive-gauge-tick-height: 2px;
    --combo-label-height: 52px;
    --bottom-panel-content-inset: 40px;
    --bench-slot-state-border: 4px;
    --bench-slot-size: 120px;               /* 신물 슬롯 등 공용 기본값 */
    --gear-slot-width: 31%;
    --summon-grid-slot-width: 188px;
    --summon-grid-slot-height: 210px;
    --summon-grid-icon-size: 112px;
    --summon-merge-badge-height: 36px;
    --panel-scrollbar-width: 16px;          /* 하단 탭 ScrollView 공용 세로 스크롤바 */
    --summon-side-width: 320px;
    --summon-contract-panel-height: 96px;
    --unit-detail-width: 760px;
    --unit-detail-height: 920px;
    --unit-detail-icon-size: 200px;
    --unit-detail-stat-row-height: 72px;
    --unit-detail-content-inset: 72px;
    --unit-detail-upgrade-button-width: 220px;
    --tutorial-card-width: 900px;
    --tutorial-card-min-height: 300px;
    --tutorial-card-inset: 48px;
    --tutorial-card-top: 220px;
    --tutorial-card-bottom: 80px;
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
- TopHUD 스테이지 정보는 프로필·재화의 Flex 폭과 분리해 HUD 가로 **50% 정중앙**에 절대 배치한다. `--top-hud-stage-width`를 유지하고 `translate: -50% 0`으로 자체 너비의 절반만큼 보정해 긴 닉네임과 겹치지 않게 한다.
- TopHUD 설정 버튼 아이콘: `Assets/Art/UIIcons/icon_settings.png` — **64×64px**로 중앙 정렬하고 버튼 텍스트는 표시하지 않는다. 터치 영역은 유지하되 버튼 배경과 테두리는 투명하게 표시한다. 원본은 `ArtSource/ui-icons/icon_settings.png`.
- 필드 우상단 설정 버튼 아래에 `×1.0 / ×1.5` 배속 토글을 둔다. ×1.5 상태는 금색 배경으로 강조하고 선택값은 `PlayerPrefs`에 저장한다. 모달 일시정지 중에는 `Time.timeScale=0`을 유지하며 마지막 일시정지 사유가 해제되면 선택했던 배속으로 복원한다.
- BottomPanel 콘텐츠 프레임: `Assets/Art/UIFrames/bottom_panel_content_frame.png` — `.tab-content`에 적용, slice left/right/top/bottom **92px**, scale **0.4**, 내부 안전 여백 **40px**. 원본은 `ArtSource/ui-frames/bottom_panel_content_frame.png`.
- 벤치·신물 공용 슬롯 프레임: `Assets/Art/UIFrames/bench_slot_frame.png` — `.bench-slot`에 적용, slice left/right/top/bottom **190px**, scale **0.1**. 기본 상태 테두리는 투명, 합성 가능 상태는 **4px** 초록 CSS 테두리를 이미지 위에 표시한다. 원본은 `ArtSource/ui-frames/bench_slot_frame.png`.
- 공용 버튼 프레임: `Assets/Art/UIFrames/button_wood_frame.png`은 `.btn`, `button_gold_frame.png`은 `.btn--primary`에 적용한다. 두 이미지 모두 slice left/right **180px**, top/bottom **160px**, scale **0.1**. 비활성 `.btn--disabled`는 배경 이미지를 제거하고 기존 회색 플랫 스타일을 사용한다. 원본은 `ArtSource/ui-frames/`에 보존한다.
- 원형 링 프레임: `Assets/Art/UIFrames/frame_ring.png` — **256×256px** 고정형(9-slice 미사용). 스킬 플로팅 버튼 128px, TopHUD 소환사 초상화 96px, SummonerTab 초상화 104px에 공용 적용하고 초상화 이미지는 링 안쪽 자식 요소에 배치한다. 원본은 `ArtSource/ui-frames/frame_ring.png`.
- 하단 탭 프레임: `Assets/Art/UIFrames/tab_button_inactive_frame.png`과 `tab_button_active_frame.png` — 모든 탭 버튼에 9-slice로 적용한다. slice left/right **56px**, top/bottom **48px**, scale **0.35**. 활성 탭은 밝은 목재와 상단 골드 라인, 비활성 탭은 어두운 목재 홈으로 구분한다. 원본은 `ArtSource/ui-frames/`에 보존한다.
- 3택 프로토타입 팝업 프레임: `Assets/Art/UIFrames/choice_modal_frame.png` — uGUI `Image.Type.Sliced`, Sprite Border left/top/right/bottom **64px**, 기준 표시 크기 **920×1450px**. 원본은 `ArtSource/ui-frames/choice_modal_frame.png`.
- 소환 룰렛 모달 외곽: `Assets/Art/UIFrames/summon_roulette_panel_frame.png` — `.summon-modal__panel`에 9-slice로 적용한다. 원본 크기 **768×1024px**, slice left/top/right/bottom **112px**, scale **0.45**, 내부 안전 여백 **72px**. 원본은 `ArtSource/ui-frames/summon_roulette_panel_frame.png`.
- 소환 룰렛 릴 창: `Assets/Art/UIFrames/summon_roulette_reel_frame.png` — `.summon-modal-reel-viewport`에 적용한다. 원본 크기 **1024×288px**, 기준 표시 비율 **32:9**, `background-size: 100% 100%`, 좌우 내부 여백 **48px**. 중앙 상·하 포인터가 늘어나므로 9-slice는 사용하지 않는다. 원본은 `ArtSource/ui-frames/summon_roulette_reel_frame.png`.
- 소환 룰렛 카드: 기본 상태는 기존 `bench_slot_frame.png`, 최종 결과는 기존 `button_gold_frame.png`를 재사용한다. 카드 기준 크기는 **180×150px**, 카드 간격은 **0px**로 서로 맞붙여 하나의 연속 릴처럼 보이게 하며 별도 단색 배경과 CSS 테두리는 두지 않는다. 확정 결과 앞뒤에 최소 3개 이상의 미끼 카드를 유지하고 결과 카드는 중앙 포인터에서 정지해야 한다.
- 하단 패널 공용 스크롤: 소환·강화·스킬·소환사 탭의 모든 `ScrollView`에 `.panel-scroll`을 붙인다. 세로 스크롤바 너비는 `--panel-scrollbar-width` **16px**, 화살표 버튼은 숨김, 트랙은 `--color-panel-deep`, 드래거는 `--color-surface-deep`과 `--color-border`를 사용한다. 가로 스크롤러는 숨긴다.
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
    --summoner-tab-profile-height: 180px; /* 소환사 탭 상단 이름·레벨·EXP 프로필 높이 */
    --summoner-stat-line-height: 64px;    /* 소환사 능력치 텍스트 한 줄 높이 */
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
   - **예외 3가지만 허용**: ① 월드 앵커 위치 동기화(배지 `style.translate`), ② 세이프 에어리어 패딩, ③ 연속 수치 게이지 fill(`.gauge__fill`의 `style.width` 또는 `style.height` — HP/EXP/세로 오버드라이브 등).
3. 상태 변형은 `--modifier` 클래스: `.badge--danger`, `.buy-button--disabled`, `.tab__button--active`, `.slot--mergeable`.
4. `:hover` 의존 금지 (모바일). 눌림 피드백은 `:active`로.
5. 전환 효과는 USS `transition`으로 선언 (예: 탭 콘텐츠 페이드, 패널 확장). DOTween은 원칙적으로 월드 오브젝트 전용이다. 단, 소환 룰렛 릴과 몬스터 드랍 골드가 HUD에 도착한 직후의 **골드 숫자 카운트업**은 값 보간만 허용하며 레이아웃·색상·위치를 직접 트윈하지 않는다.
6. `common.uss`: 버튼/캡슐/레드닷 등 2곳 이상에서 쓰는 공용 클래스만. 한 화면 전용 스타일은 그 화면의 USS로.

## 6. UXML 규칙

1. 트리 구조는 SPEC §3.2에서 벗어나지 않는다. RootLayout이 TopHUD/FieldOverlay/BottomPanel을 `<ui:Instance>`(Template)로 포함.
2. 반복 요소(강화/스킬 행, 벤치 슬롯)는 **템플릿 1개 + 런타임 Instantiate**. 복붙으로 행을 늘리지 않는다. 소환사 능력치와 특성은 버튼형 반복 행이 아니라 정적 정보 레이아웃과 텍스트 바인딩을 사용한다.
3. UXML에 인라인 `style` 속성 금지 (레이아웃 프로토타입도 USS로).
4. FieldOverlay는 `position: absolute`, `picking-mode="Ignore"` 기본 — 스킬 버튼 등 상호작용 요소만 개별적으로 피킹 허용. 필드 터치를 UI가 가로채면 안 된다.
5. 하단 패널 존 비율은 flex로: 상단 HUD ~8% / 필드 ~50% / 하단 패널 ~40% (SPEC §4.1). 픽셀 고정 높이는 행/탭/소환사 탭 프로필 같은 내부 요소에만.

### 성장 UI 배치 규칙 (2026-07-18 사용자 지시)

- `UpgradeTab`은 저장되는 공격력·공격속도·회복·치명타 강화만 표시한다. 소환사·unitId별 슬라임 레벨업 행은 표시하지 않는다.
- unitId별 슬라임 레벨업 행은 `UpgradeTab`에 표시하지 않는다. 보유 슬라임 슬롯을 탭해 연 상세 팝업 안에서만 공통 레벨·현재→다음 공격력/공격속도·골드 비용을 보여주고 강화한다.
- 상세 팝업의 슬라임 레벨업은 같은 unitId의 벤치·필드·모든 머지 등급에 공통 적용된다. 골드 부족 또는 최대 레벨이면 버튼을 비활성화하되 비용 또는 `MAX` 표기는 유지한다.
- 슬라임 상세 팝업의 높이는 **1120px**, 강화 버튼 최소 너비는 `--unit-detail-upgrade-button-width` **220px**로 사용한다.
- 소환사 EXP는 획득 즉시 요구량을 연속 차감해 가능한 만큼 자동 레벨업한다. 레벨업 버튼은 어느 탭에도 표시하지 않는다.
- `SummonerTab`은 버튼·`UpgradeRow`·나무 판재 강화 행을 사용하지 않는 정보 화면이다. 상단에 소환사 이름·현재 Lv·EXP를 표시하고, 아래에는 실제 적용 중인 공격력·공격속도·최대 HP·치명타·★2 직행 확률과 영구/런 특성을 일반 텍스트로 표시한다.
- 소환사 정보 프로필 높이는 **180px**, 능력치 텍스트 한 줄 최소 높이는 `--summoner-stat-line-height` **64px**로 사용한다.

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
6. 길게 누르기(연속 강화): 짧은 탭은 `Button.clicked`로 1회 처리하고, PointerDown 후 0.4s 지연 → 0.08s 간격 반복한다. PointerUp만으로 짧은 탭을 판정하지 않으며, ScrollView가 포인터 캡처를 가져가도 일반 클릭 경로가 유실되지 않게 공용 헬퍼 `LongPressRepeater`로 구현한다.

## 8. 월드 ↔ UI 브릿지 (스파이크 A/B의 규격)

### 8.1 필드 상단 웨이브 상태
- 현재 웨이브와 잔여 몬스터 수는 TopHUD 아래의 FieldOverlay 상단 중앙에 표시한다.
- 방향 예고 배지(N/E/S/W)는 사용하지 않는다. FieldOverlay는 웨이브 상태·콤보·오버드라이브 게이지와 스킬 플로팅 버튼을 소유한다.
- 콤보 라벨은 웨이브 상태 바로 아래에서 2콤보부터 표시한다. `xN COMBO` 단일 라벨을 재사용하고 10/20/30 진입 순간에만 `.combo--milestone` 클래스로 강조한다. 처치마다 새 Label을 생성하지 않는다.
- 오버드라이브 게이지는 스킬 버튼 왼쪽에 버튼과 같은 바닥선으로 `--overdrive-gauge-width` × `--overdrive-gauge-height` 세로 연료 탱크처럼 배치한다. 버튼과의 간격은 `--gap-md`이며, fill은 아래에서 위로 차고 25% 단위 내부 구획선 3개를 둔다. 상태 문구는 폭이 좁은 탱크 안이 아니라 바로 위 `--overdrive-status-width` × `--overdrive-status-height` 라벨에 표시한다. 충전/준비/활성 상태는 `.overdrive--charging`, `.overdrive--ready`, `.overdrive--active` 클래스로 나누고 fill 높이만 런타임에서 갱신한다.

### 8.2 벤치(UI) → 필드(월드) 드래그 앤 드롭
- 흐름: 벤치 슬롯 `PointerDownEvent` → 루트에 고스트 요소 생성(`picking-mode: Ignore`) → `PointerMoveEvent`로 고스트 이동 + 포인터 캡처 → `PointerUpEvent`에서 스크린 좌표로 `Physics2D.OverlapPoint`(슬롯 레이어) → 유효 슬롯이면 스냅 배치, 아니면 벤치 복귀.
- 드래그 중 필드 슬롯 하이라이트는 월드 쪽(슬롯 SpriteRenderer 틴트)에서 처리.

## 9. 인터랙션 필수 문법 (키우기 문법 — SPEC §4)

- **웨이브 상태**: HUD 아래 필드 상단 중앙에 현재 웨이브(예: `WAVE 12`)와 잔여 몬스터 수를 함께 표시한다. 전체 웨이브 한계는 표시하지 않는다.
- **레드닷**: 탭 버튼 우상단 12px 원, `--color-reddot`. 강화 가능/새 신물 시 표시. 공용 클래스 `.reddot`.
- **탭 전환**: 탭 열면 하단 패널 확장 + 필드 축소(USS transition), 전투는 계속 보이게.
- **소환 탭**: 좌측은 벤치 대기 + 필드 배치 개체를 합친 전체 보유 유닛을 최대 12슬롯 **3열 × N 세로 스크롤 그리드**로 배치하고, 현재 잠긴 슬롯은 숨긴다. 같은 유닛 + 같은 머지 등급은 슬롯 하나로 묶고 좌상단에 전체 `xN`, 본문에 유닛 공통 `Lv.N`과 화면 성급 `★1~★3`을 표시한다. 슬롯과 상세 팝업 이미지는 대표 인스턴스의 `WorldSpriteAtRank(rank)`를 사용해 현재 성급 이미지와 일치시킨다. 내부 `Rank 0~2` 값은 UI에 직접 노출하지 않는다. 헤더는 `보유 m/n · 슬롯 s개` 형식이며 보유 한도는 벤치와 필드의 총 개체 수를 센다. 수량 2개 이상인 ★3 미만 스택은 합성 가능 상태로 표시한다. 우측은 `용병 계약서 n장`·★2 직행 확률·`소환 ×1` 버튼을 세로 배치한다. 해금된 8종 전체를 선택 슬롯처럼 상시 나열하지 않는다.
- **소환 슬롯 탭/드래그 분리**: 포인터 이동이 드래그 임계값 미만이면 유닛 상세 팝업을 연다. 임계값 이상일 때 해당 스택에 벤치 대기 개체가 있으면 그 인스턴스 1개로 기존 벤치→필드 배치를 시작하고, 필드 배치 개체만 있으면 드래그를 시작하지 않는다. 상세 팝업은 전체 스택 수량·공통 강화 레벨·머지 등급·속성·공격 방식과 강화 배율까지 반영한 현재 전투 수치를 표시하며 열려 있는 동안 필드 클릭 공격을 차단한다. ★1·★2는 `★3 해금 · 스킬명`, ★3은 `스킬명 · N초` 형식으로 자동 특수 스킬과 쿨다운을 기존 능력치 행 스타일에 표시한다.
- **소환 버튼**: 우측 엄지 위치(패널 우하단), 용병 계약서 1장 비용과 등급 확률을 노출한다. 계약서가 0장이거나 총 보유 개체 수가 현재 슬롯 한도에 도달하면 회색 비활성화한다.
- **합성 가능 벤치 슬롯**: `--color-positive` 테두리 + "합성" 뱃지.
- **강화 탭**: 전체 공격력·전체 공격속도·소환사 HP 회복·치명타 저장 강화 4행만 표시한다. 각 행은 현재→다음 수치와 골드 비용을 함께 표시하며, 골드 부족·최대 레벨·HP 회복이 불필요한 상태에서는 버튼을 회색 비활성화한다. 강화 버튼은 짧게 누르면 1회, 0.4초 이상 누르면 0.08초 간격으로 반복 시도한다.
- **스킬 탭**: `UpgradeRow` 템플릿으로 소환사 보호막·군단 지휘·생명의 가호·정령 공명·시간 가속 5행을 표시한다. 상단에 현재 `N/3 장착`을 표시하고, 이름 아래에는 효과·쿨다운 또는 `Lv.N 해금`을 표시한다. 해금된 행은 `장착`/`해제`, 잠긴 행은 `잠김`, 세 슬롯이 찬 상태의 미장착 행은 `3/3`으로 비활성화한다. 별도 스킬 레벨업 버튼은 두지 않는다.
- **필드 스킬 버튼**: 우하단 128px 링은 장착 공격 신물의 스킬 이름·아이콘·남은 쿨다운과 대상 지정 상태를 표시한다. 그 왼쪽에는 `--buff-skill-button-size` 96px 소환사 버프 버튼 3개를 가로 배치하며 각 버튼은 아이콘·남은 쿨다운·활성 상태를 표시한다. 비어 있는 슬롯은 비활성화한다. 대상 범위와 얼음벽 방향 미리보기는 UI Toolkit이 아니라 월드 `SpriteRenderer`로 그린다.
- **콤보/오버드라이브**: FieldOverlay에서 콤보 라벨과 오버드라이브 게이지를 소유한다. 콤보는 2부터 노출하고 10/20/30 진입 때만 강조한다. 게이지는 스킬 버튼 왼쪽의 48×128px 세로 연료 탱크이며 아래에서 위로 충전되고 25% 구획선을 표시한다. 충전 중 수치 텍스트를 숨기고, 가득 차면 탱크 위에 `READY`, 활성 중에는 `OVERDRIVE N.Ns`를 표시한다. 오버드라이브 색은 `--color-overdrive*`만 사용하며 골드 재화/행동 색과 섞지 않는다.
- **도감 버튼 스택**: FieldOverlay의 TopHUD 아래 우상단에 **200×96px** 버튼 2개를 세로 배치한다. 위는 `슬라임 도감`, 아래는 `몬스터 도감`이며 버튼 간격은 `--gap-md`를 사용한다.
- **슬라임 도감**: 전체화면 스크림 안의 920×1450 패널에 소환 풀 8종을 4열 슬롯으로 배치한다. 잠긴 슬롯은 `?`와 `Lv.N 해금`, 해금 슬롯은 ★1 이미지·이름을 표시한다. 상세 영역은 속성·희귀도·공격 방식, ★1 HP·공격력·공속·사거리·이동속도와 ★3 스킬명·쿨다운을 보여준다. 팝업 동안 `GameplayPauseReason.SlimeCodex`로 게임플레이를 정지한다.
- **몬스터 도감**: `몬스터 도감` 버튼에서 연다. 전체화면 스크림 안의 920×1450 패널에 4×4 슬롯 그리드와 상세 영역을 배치한다. 미조우 슬롯은 `?`만 표시하고 팝업 동안 게임플레이를 정지한다.
- **행상인**: RootLayout 내부 전체화면 팝업으로 장비·소모품·전리품 카드 3장을 세로 배치한다. 가격·효과·품절·구매 불가 사유를 항상 표시하고 닫기 버튼을 제공한다.
- **전체화면 모달 호스트**: `RootLayout`에 `<Instance>`로 삽입하는 모달은 래퍼 `TemplateContainer`에 `.modal-host`와 `picking-mode="Ignore"`를 적용한다. 래퍼는 화면 전체 영역을 유지하되 직접 터치 대상이 되지 않고, 열린 모달의 자식 버튼만 입력받아야 한다. 일시정지를 동반하는 모달은 닫힘 상태에 공용 `.hidden`(`display: none`) 대신 `.modal-overlay--hidden`(`visibility: hidden`)을 사용해 첫 표시 전에도 화면 크기 레이아웃을 유지한다. Play Mode 검사에서 닫힌 오버레이와 호스트가 모두 루트 높이의 90% 이상이고 호스트 `pickingMode`가 `Ignore`인지 확인한다.
- **가방 탭**: 내부를 `장비 / 신물 / 전리품`으로 구분한다. 장비는 무기·방어구·장신구 슬롯과 보유 목록, 신물은 장착 신물과 고유 공격 선택, 전리품은 이번 도전의 행상인 전리품과 5웨이브 특성을 표시한다. 내부 `Equipment*`·`RunRelic*` 타입과 저장 키는 호환을 위해 유지할 수 있다.
- **소환사 영구 성장**: 소환사 EXP는 획득 즉시 자동으로 레벨에 반영한다. 소환사 탭은 저장된 영구 Lv와 현재 EXP/요구 EXP 게이지, 실제 적용 능력치와 특성 텍스트만 표시하며 레벨업 버튼이나 공용 강화 행을 사용하지 않는다.
- **터치 타깃**: 상호작용 요소는 최소 `--touch-min`(96px) 확보.
- **첫 런 튜토리얼**: `RootLayout`의 단일 `TutorialOverlay` 인스턴스를 재사용한다. 행동 단계에서는 카드만 입력을 받고 필드·소환 버튼 입력을 통과시키며, 설명 확인 단계에서만 전체 입력을 막는다. 안내 카드는 `--tutorial-card-*` 토큰을 사용하고 소환·재배치·필드 2머지의 실제 게임 이벤트로 다음 단계가 열린다.

## 10. Do / Don't 요약

### 슬롯 릴 애니메이션 예외

- 소환 룰렛 릴은 제한된 UI 애니메이션 예외로, `SummonRouletteView`가 런타임 생성 `summon-reel-strip`의 `style.translate`와 결과 카드의 `style.scale`을 DOTween `DOTween.To`로 보간할 수 있다. 결과 카드는 배열 끝이 아닌 중간에 배치하고, 스킵 여부와 관계없이 중앙 포인터에 동일하게 정렬한다.
- 릴은 기존 `BottomPanel` 내부에만 두며 별도 UIDocument나 Canvas를 만들지 않는다. 결과는 애니메이션 시작 전에 게임 로직이 확정한다.
- 소환 결과를 시작하면 소환 룰렛 일시정지 사유를 획득해 월드와 웨이브를 멈추고, 결과 확정 콜백에서 해제한다. 릴 이동·결과 강조·모달 닫힘 지연은 DOTween independent update로 실행해 정지 중에도 진행한다.

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
- 액티브 스킬 아이콘은 `icon_skill_meteor.png`, `icon_skill_ice_wall.png`, `icon_skill_aegis.png`를 사용한다. 모두 **128×128px** 투명 PNG·Point 필터·무압축으로 임포트하고, `SkillTab`의 88px 공용 행 아이콘과 FieldOverlay 스킬 버튼의 `--skill-button-icon-size` **56px** 영역에 같은 에셋을 재사용한다.
- 하단 5개 탭은 `icon_tab_summon.png`, `icon_tab_upgrade.png`, `icon_tab_skill.png`, `icon_tab_gear.png`, `icon_tab_summoner.png`을 사용한다. 모두 **128×128px** 투명 PNG이며 **44px**로 표시하고, 탭 텍스트는 기능명 확인을 위해 유지한다.
