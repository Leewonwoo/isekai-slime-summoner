# AI 활용 내역 로그 (영역별)

기술 문서 PDF의 "코드·에셋·밸런싱·사운드 영역별 활용 내역" 재료. **의미 있는 AI 작업을 한 날마다 해당 영역에 한 줄 추가한다.**
형식: `날짜 | 도구 | 무엇을 | 대표 프롬프트 요지 (긴 프롬프트는 prompt-cases/ 링크) | 결과물 위치`

## 코드 (Claude Code)

| 날짜 | 무엇을 | 프롬프트/지시 요지 | 결과물 |
|---|---|---|---|
| 07-14 | 소환 경제를 골드에서 용병 계약서로 분리하고 소환 탭 UX 개편 | "웨이브 마무리마다 주는 스크롤로 소환" — 골드는 강화 전용, 계약서는 시작·웨이브 클리어 보상으로 지급. 실제 보유 유닛 12슬롯과 우측 계약서·확률·소환 액션 구조로 정리 | docs/SPEC.md, docs/ui-guidelines.md, Assets/Scripts/Core/, Assets/Scripts/Data/StageTimeline.cs, Assets/UI/UXML/tabs/SummonTab.uxml, Assets/UI/USS/BottomPanel.uss |
| 07-14 | 골드 아이콘 단일 주화 리디자인 | 몬스터 드랍 가독성을 높이기 위해 기존 겹친 주화 3개를 단일 두꺼운 금화로 편집. 기존 문양·팔레트·픽셀 스타일 유지 후 크로마키 제거 | Assets/Art/UIIcons/icon_gold.png; 원본 ArtSource/ui-icons/ |
| 07-14 | TopHUD 골드·젬 아이콘 적용 | 기존 재화 캡슐의 색상 원형 플레이스홀더를 실제 투명 PNG 아이콘 참조로 교체하고 공통 크기 토큰을 추가 | Assets/UI/USS/common.uss, Assets/UI/USS/variables.uss, docs/ui-guidelines.md |
| 07-14 | 골드·젬 UI 아이콘 생성 및 크로마키 배경 제거 | Cross Defense의 기존 16-bit 픽셀 아트와 UI 색상 토큰에 맞춘 자원 아이콘 2종 생성. 초록 크로마키를 제거해 알파 PNG로 변환 | Assets/Art/UIIcons/icon_gold.png, Assets/Art/UIIcons/icon_gem.png; 원본 ArtSource/ui-icons/ |
| 07-11 | Android APK 빌드 파이프라인 자동 세팅 (플랫폼 전환, Player Settings, 프로브 씬, 빌드) | "APK 기준 모바일 빌드로 파이프라인 검증" — Claude Code가 Unity 에디터를 MCP로 직접 제어 | 커밋 `ee2bf1f`, Builds/CrossDefense-dev.apk |
| 07-11 | 프로젝트 문서 체계 수립 (SPEC/CLAUDE.md/에셋 대장/UI 지침) | 스펙 전문 전달 → 기준 문서·작업 규칙·디자인 토큰 문서화 | 커밋 `7503eb9`, `8c34853` |
| 07-11 | 테마·유닛 로스터·상성·소환/머지 루프 기획 확정 (소환사/고블린/슬라임, 3속성 상성 고리, 종류 랜덤 소환+4단 성급 머지, 3뷰 시트 생성 전략) | "인간 소환사 + 고블린 유닛 + 슬라임 적, 상성 포함" / "운빨 소환 + 3개 머지" → Claude가 기존 스펙(4레인 딜레마·틴트 규칙·에셋 예산)과 정합성 맞춰 설계 | SPEC.md §1·2.2·2.3·2.7·2.8·5.1·5.3 |
| 07-11 | UI Toolkit 스캐폴딩 전체 구축 (UXML 10개·USS 7개·컨트롤러 6개, PanelSettings, 씬 연결) | "ui-guidelines.md 규격대로 UI/UX 구성·구축" — Claude Code가 파일 생성부터 플레이 모드 스크린샷 검증까지 수행 | Assets/UI/, Assets/Scripts/UI/ |
| 07-11 | 메인 UI 스킨 디자인·적용 2회전 — v1 "고블린 캠프"(다크 우드×골드) → 레퍼런스 대조 후 v2 "척박한 숲" 픽셀(돌 프레임×나무 판재)로 피벗. HTML 목업 → ui-guidelines §4 토큰 개정 → USS 전면 리스킨 → 플레이 모드 캡처 검증 | "메인 ui/ux 디자인 하나 해서 적용" → 레퍼런스 스크린샷 제시로 방향 재확정 | Assets/UI/USS/, docs/ui-guidelines.md §4, 아티팩트 목업 v2 |
| 07-11 | 스타일 앵커 v1(SD 셀셰이딩)→v2(16-bit 픽셀·척박한 숲) 피벗 + 배경 4종·UI 9-slice 프레임 2종 프롬프트 설계 (십자 지형/슬롯 타일/소환석 피격 3단계 시트/스폰 지점/돌 패널/나무 버튼) | 레퍼런스 기반 스타일 그라운딩 — 사례: prompt-cases/case-01-style-anchor | SPEC §5.1 v2, docs/prompts/environment-prompts.md (생성 후 asset-ledger.csv 기록 예정) |
| 07-11 | UI 에셋 프롬프트 팩 설계 — 확정된 배경 1호(ingame_stage_1.png) 톤을 역추출해 9-slice 프레임 5종 + 아이콘 시트 4장(재화/스탯/속성/스킬·장비) 프롬프트 작성 | 생성 결과물 기반 팔레트 그라운딩 + 그리드 시트 트릭 | docs/prompts/ui-prompts.md |
| 07-11 | 고블린 유닛 8종 프롬프트 팩 설계 — 3뷰 시트 템플릿 1개 + 개체 변수 8종, 종족 일관성용 베이스 서술 고정 문구, 실루엣 분리 원칙 | 베이스 서술 앵커 고정 + 템플릿-변수 분리(3단 아키텍처 레이어 2/3) | docs/prompts/unit-prompts.md |
| 07-13 | TopHUD 구조 개편 — 소환사 프로필·스테이지명·웨이브/잔여 몬스터·재화·설정 배치, N/E/S/W 방향 배지 제거 | "이미지 생성 전 Top UI 구조 변경" — UI 가이드의 토큰/데이터 바인딩 패턴에 맞춘 UXML·USS·C# 리팩터링 | Assets/UI/UXML/TopHUD.uxml, Assets/UI/USS/TopHUD.uss, Assets/Scripts/UI/TopHUDController.cs |
| 07-14 | 웨이브 상태를 TopHUD에서 필드 상단 오버레이로 이동하고 표시 간소화 | "wave가 HUD에 들어가있거든? 아래로 빼줘야해" / "현재 웨이브만 딱 보여주면" — MCP로 UIDocument 트리를 확인한 뒤 FieldOverlay 상단 중앙에 배치, 전체 웨이브 표기·테두리·배경 제거 | Assets/UI/UXML/FieldOverlay.uxml, Assets/UI/USS/FieldOverlay.uss, Assets/Scripts/UI/FieldOverlayController.cs |
| 07-14 | TopHUD 배경 프레임 생성 및 UI Toolkit 9-slice 적용 | 16-bit pixel-art 가로형 HUD 프레임 — 낡은 석재 테두리, 어두운 목재 내부, 절제된 금색 포인트, 중앙 텍스트 영역 비움. 외곽 검정 영역만 투명화해 목재 질감 보존 | ArtSource/ui-frames/top_hud_frame.png → Assets/Art/UIFrames/top_hud_frame.png; Assets/UI/USS/TopHUD.uss |
| 07-14 | 그룹 A 실행 기반 구현 — StageTimeline 런타임 실행, 외곽 스폰, 몬스터 이동·소환사 피해, 씬·HUD 연결 | 기존 방향 레인 규칙을 화면 외곽 스폰 구역으로 전환하고, 임의 웨이브 수를 실행하는 데이터 기반 런타임 골격 구현 | Assets/Scripts/Core/GameManager.cs, WaveManager.cs, MonsterSpawner.cs, Assets/Scripts/Units/MonsterController.cs, Assets/Scripts/Data/StageTimeline.cs, Assets/Tests/EditMode/StageTimelineTests.cs |

## 에셋 (이미지 — ChatGPT 이미지 생성)

| 날짜 | 무엇을 | 프롬프트 | 결과물 |
|---|---|---|---|
| 07-14 | 강화 스탯 아이콘 6종 시트 생성·슬라이스·적용 | 기존 골드·젬·설정 아이콘을 스타일 레퍼런스로 사용해 3×2 시트(공격력/공속/치명타/HP 회복/사거리/골드 획득)를 생성하고 투명 128px 아이콘으로 분리 | ArtSource/ui-icons/icons_stat_sheet.png → Assets/Art/UIIcons/icon_atk·aspd·crit·hp·range·income.png; UpgradeRow 강화 탭 4종 적용 |
| 07-14 | 원형 돌·골드 링 프레임 생성 및 공용 적용 | 기존 슬롯·버튼·TopHUD 프레임을 스타일 레퍼런스로 사용해 얇은 풍화 석재 링과 동서남북 금색 리벳을 생성. 크로마 제거 후 고정형 256px로 가공 | ArtSource/ui-frames/frame_ring.png → Assets/Art/UIFrames/frame_ring.png; 스킬 플로팅 버튼·TopHUD/SummonerTab 소환사 초상화 프레임 |
| 07-14 | 설정 버튼 톱니 아이콘 생성·배경 제거·TopHUD 적용 | 낡은 석재·목재 HUD에 맞춘 16-bit 픽셀 아트 8톱니 골드 기어, 초록 크로마키 배경, 버튼 판·텍스트·그림자 제외 | ArtSource/ui-icons/icon_settings.png → Assets/Art/UIIcons/icon_settings.png; TopHUD 설정 버튼에 64px 중앙 정렬 적용 |
| 07-14 | BottomPanel 콘텐츠 프레임 생성 및 UI Toolkit 9-slice 적용 | TopHUD 프레임을 스타일 레퍼런스로 사용해 얇은 낡은 돌 테두리와 어두운 흙빛 석판 내부를 가진 2:1 풀블리드 콘텐츠 프레임 생성. 탭·버튼·슬롯·텍스트는 제외 | ArtSource/ui-frames/bottom_panel_content_frame.png → Assets/Art/UIFrames/bottom_panel_content_frame.png; Assets/UI/USS/BottomPanel.uss |
| 07-14 | 벤치·장비 공용 슬롯 프레임 생성 및 적용 | BottomPanel 콘텐츠 프레임을 스타일 기준으로 낡은 돌 테두리와 어두운 오목 홈을 가진 1:1 빈 슬롯 생성. 합성 가능 상태는 별도 초록 CSS 테두리로 유지 | ArtSource/ui-frames/bench_slot_frame.png → Assets/Art/UIFrames/bench_slot_frame.png; Assets/UI/USS/BottomPanel.uss |
| 07-14 | 공용 목재·골드 버튼 프레임 2종 생성 및 9-slice 적용 | TopHUD 목재와 BottomPanel 석재 팔레트를 기준으로 일반 행동용 낡은 목재 버튼을 생성하고, 같은 구조의 골드 주요 행동 변형을 제작. 중앙 런타임 텍스트 영역은 비움 | ArtSource/ui-frames/button_wood_frame.png, button_gold_frame.png → Assets/Art/UIFrames/; Assets/UI/USS/common.uss |
| 07-14 | 중복 SummonerStrip 제거 및 소환사 탭으로 프로필 통합 | TopHUD와 최하단 스트립의 소환사 정보 중복을 제거하고, 초상화·Lv·EXP·강화 기능을 SummonerTab 상단 프로필로 이동해 탭 콘텐츠 높이 확보 | Assets/UI/UXML/BottomPanel.uxml, Assets/UI/UXML/tabs/SummonerTab.uxml, Assets/UI/USS/BottomPanel.uss, docs/SPEC.md, docs/ui-guidelines.md |
| 07-11 | 인게임 메인 필드 배경 확정 (십자 지형 통바닥, 첫 생성 에셋 — 전체 아트 톤 기준) | [environment-prompts.md](prompts/environment-prompts.md) §1 필드 통바닥 v1 | ArtSource/core/ingame_stage_1.png (1254px 원본) → Assets/Art/ (최근접 1/4 다운스케일 314px, Point/무압축/PPU 100 임포트 — 후처리·임포트는 Claude Code) |
| 07-12 | 메인 소환사(=코어) 전신 스프라이트 확정 + 필드 중앙 배치 | [environment-prompts.md](prompts/environment-prompts.md) §3b 소환사 전신 계열 (정장 차림 변형) | ArtSource/units/MAIN_SUMMONER.png → Assets/Art/ (flood fill 배경 제거·크롭·1/4 다운스케일 129×153px — 후처리 스크립트·씬 배치는 Claude Code) |
| 07-13 | 고블린 숲·이세계 귀환 의식 콘셉트의 인게임 맵 1차 시안 생성 | 이세계 생존 디펜스용 고블린 숲 전장 배경 — 16-bit pixel art, top-down four-direction crossroads, central summoning rune, dormant purple return portal, four goblin trail entrances | ArtSource/core/ingame_stage_2.png → Assets/Art/ingame_stage_2.png (314×314px 최근접 다운스케일, 비채택 시안) |
| 07-13 | 십자형 레인과 장식 프롭을 제거한 폐쇄형 고블린 숲 맵 2차 시안 채택 | 기존 맵 시안 편집 — 숲 바닥 전체를 플레이 공간으로 사용하고 사방에서 몬스터가 등장하는 구도 | ArtSource/core/ingame_stage_3.png → Assets/Art/ingame_stage_3.png (314×314px 최근접 다운스케일, SampleScene 배경 교체) |
| 07-14 | 고블린 아군에서 슬라임 아군으로 피벗 후 첫 스타일 앵커 생성 | [slime-prompts.md](prompts/slime-prompts.md) §2 주먹 슬라임 — strict 16-bit pixel art, flat chroma-key, mobile-readable silhouette | ArtSource/units/unit_punch_slime.png → Assets/Art/Units/unit_punch_slime.png (크로마 제거·알파 크롭·128×128px 최근접 다운스케일, Unity MCP 재임포트) |
| 07-14 | 슬라임 기본형 얼굴 제거 및 성급별 표정 규칙 확정 | 기본·★1은 눈·입 없음, ★2부터 눈·입 추가, ★3은 개성 표정. image-to-image 편집과 negative prompt 사용 | docs/prompt-cases/case-03-slime-face-evolution/, Assets/Art/Units/unit_punch_slime.png v2 |
| 07-14 | 주먹 슬라임 기본 몸체에서 팔·주먹 제거, 공격은 별도 스프라이트+DOTween으로 전환 | 기본 몸체는 얼굴·사지 없는 단순 실루엣, 공격 모션은 프레임 시트 대신 별도 주먹 스프라이트를 전진·복귀시키는 방식 | docs/prompts/slime-prompts.md §4, docs/prompt-cases/case-03-slime-face-evolution/v3.png, Assets/Art/Units/unit_punch_slime.png v3 |
| 07-14 | 주먹 슬라임 이동용 3×3 9프레임 포잉포잉 스프라이트 시트 생성 | 이동에 한해 프레임 애니메이션을 허용하도록 SPEC 개정. 동일 몸체의 squash·stretch·settle 루프 | ArtSource/units/unit_punch_slime_move_sheet.png → Assets/Art/Units/unit_punch_slime_move_sheet.png (384×384px, 프레임당 128×128px) |
| 07-14 | 주먹 슬라임 기본 공격용 3×3 9프레임 스프라이트 시트 생성 | 공격 시 임시 주먹이 천천히 돌출·유지·복귀하도록 9프레임 구성. 기본 몸체는 얼굴·사지 없는 상태 유지 | ArtSource/units/unit_punch_slime_attack_sheet.png → Assets/Art/Units/unit_punch_slime_attack_sheet.png (384×384px, 프레임당 128×128px) |
| 07-14 | 주먹 슬라임 공격 주먹 형태를 양손형으로 개선 | 첨부 레퍼런스 기반 image-to-image 편집, reference-image conditioning과 invariant locking으로 양쪽 짧은 팔·뭉툭한 주먹을 고정하고 단일 긴 돌기를 제거 | docs/prompt-cases/case-04-punch-shape/, unit_punch_slime_attack_sheet.png v2 |
| 07-14 | 주먹 슬라임 공격 콘티 기반 단일 주먹 동작으로 재생성 | 사용자가 그린 3×3 러프 콘티를 sketch-to-image/reference conditioning으로 반영. 오른쪽 한쪽 팔·주먹의 준비→대각선 돌출→회수 타이밍을 9프레임으로 고정 | docs/prompt-cases/case-04-punch-shape/v3.png, unit_punch_slime_attack_sheet.png v3 |

> 개별 에셋은 [asset-ledger.csv](asset-ledger.csv)에 전수 기록. 여기에는 배치 작업 단위로 요약.

## 밸런싱 (LLM CSV 생성)

| 날짜 | 무엇을 | 프롬프트 요지 | 결과물 |
|---|---|---|---|
| | | | |
| 07-13 | Stage Timeline ScriptableObject and Unity editor window implementation | User requirement: editable stage wave count, monster composition, and balance overrides from a direct editor menu | Assets/Scripts/Data/StageTimeline.cs, Assets/Scripts/Data/MonsterData.cs, Assets/Editor/StageTimelineEditorWindow.cs |

## 사운드 (Suno / ElevenLabs / jsfxr)

| 날짜 | 무엇을 | 프롬프트 | 결과물 |
|---|---|---|---|
| | | | |
