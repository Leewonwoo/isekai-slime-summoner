# Isekai Slime Summoner

NAN 2026 (NHN Game × AI Hackathon) 사전 과제. 1인 개발, 마감 **2026-08-10**.

**전체 스펙(단일 기준 문서): [docs/SPEC.md](docs/SPEC.md)** — 게임 설계·일정·제출물 관련 판단은 반드시 이 문서를 따른다.

**UI 작업 시 필독: [docs/ui-guidelines.md](docs/ui-guidelines.md)** — UXML/USS/컨트롤러 작성 전 반드시 이 지침을 따른다 (디자인 토큰, 네이밍, 컨트롤러 패턴, 월드↔UI 브릿지 규격). 지침에 없는 값이 필요하면 문서에 먼저 추가하고 코드를 쓴다.

## 프로젝트 개요

십자형 타워 디펜스 × 로그라이크 × 키우기. **테마: 인간 소환사가 고블린 용병단(유닛 8종, 정면/후면/측면 3뷰 스프라이트)을 소환해 슬라임 군단(형태 4종 × 속성 틴트)을 막는다. 속성 상성 4종(무/화염/빙결/자연, 화>자연>빙>화) — SPEC §2.3/2.7/2.8.** 중앙 소환사(=코어, 소환석 구조물 폐기 — SPEC §2.1) + 4방향 레인(레인당 슬롯 3칸), 운빨 소환(종류 랜덤 + ★1 직행 잭팟)·3머지(기본→★1→★2→★3, SPEC §2.2)·재배치. 웨이브 20개 + 보스. 웨이브는 **규칙 기반**(SO 웨이브 테이블 + 방향 가중치 + 랜덤 시드 변주) — **런타임 LLM API 호출은 스코프 아웃**, AI는 개발 도구(코드·에셋·밸런싱 CSV 생성)로만 사용.

## 기술 스택 / 환경

- Unity **6000.3.5f2**, 2D URP 템플릿, Input System
- 빌드 타깃: **Android APK (모바일 빌드)** — 2026-07-11 사용자 결정으로 WebGL에서 변경
  - 세로 고정 (Portrait, 1080×1920 기준), IL2CPP + ARM64, 패키지 `com.wonucode.crossdefense`
  - ⚠️ SPEC §3/§7의 원래 제출 요건은 "링크 클릭만으로 브라우저 플레이"(WebGL) — 접수 요강에서 APK 허용 여부 확인 필요
- UI: **UI Toolkit** (HUD/하단 패널/배지) + 월드 오브젝트 SpriteRenderer (필드/유닛/몹/투사체)
  - 필드를 UI Toolkit으로 그리지 말 것
  - UXML 구조는 SPEC §3.2 고정 (RootLayout / TopHUD / FieldOverlay / BottomPanel), USS는 §3.3 (variables.uss CSS 변수, 하드코딩 색상 금지)
- 연출: 프레임 애니메이션 금지, 정적 스프라이트 + DOTween 코드 연출
- 데이터: ScriptableObject (웨이브/유닛/강화), 오브젝트 풀링 (몹/투사체)

## 작업 순서 (SPEC §3.4 — 코어 루프 착수 전 선행)

1. ~~빌드 파이프라인 검증~~ ✅ (2026-07-11, Android APK 빌드 성공)
2. UI 스캐폴딩: 폴더 구조 + PanelSettings + RootLayout/TopHUD/BottomPanel 빈 껍데기 + variables.uss
3. 기술 스파이크 A: 벤치(UI) → 필드(월드) 드래그 앤 드롭 브릿지
4. 기술 스파이크 B: 월드 앵커 배지 WorldToScreenPoint 동기화
5. 코어 루프 (스폰/이동/공격/웨이브)

## 작업 규칙

- **커밋 메시지에 AI 활용 흔적 남기기** (예: `feat: AI-generated unit sprites integrated`) — 커밋 기록 자체가 제출물 증거
- AI 생성 에셋을 추가할 때마다 [docs/asset-ledger.csv](docs/asset-ledger.csv)에 기록 (파일명/도구/프롬프트/라이선스)
- 이미지 원본(크로마키 배경)은 `ArtSource/`에, 가공 완료본만 같은 파일명으로 `Assets/Art/`에 (규칙: [ArtSource/README.md](ArtSource/README.md))
- **의미 있는 AI 작업(코드/에셋/밸런싱/사운드)을 한 날마다 [docs/ai-usage-log.md](docs/ai-usage-log.md)에 한 줄 추가** — 기술 문서 PDF의 직접 재료. Claude Code(나)도 큰 작업 마무리 시 스스로 기록할 것
- 프롬프트 실패→개선(v1→v2)을 겪으면 그날 바로 `docs/prompt-cases/`에 사례 기록 (양식: [prompt-cases/README.md](docs/prompt-cases/README.md), **사용 기법명 필수**)
- 스코프 아웃 항목(SPEC §8) 추가 금지 — 특히 런타임 LLM 호출. 고민되면 자르는 쪽으로
- 4주차(8/4~)는 기능 동결 — 폴리싱/밸런싱/문서만

## 디렉터리 구조 (SPEC §3.1)

```
Assets/
  Scripts/
    Core/        # GameManager, WaveManager, EconomyManager
    Units/       # 유닛/몹/투사체 (월드 오브젝트)
    UI/          # UI Toolkit 컨트롤러 (C#)
    Data/        # ScriptableObject 정의
  UI/
    UXML/        # 레이아웃
    USS/         # 스타일
  Data/          # SO 인스턴스 (유닛, 웨이브, 강화 테이블)
  Art/           # AI 생성 스프라이트 (에셋 대장과 파일명 일치)
  Audio/
docs/
  SPEC.md        # 단일 기준 문서
  asset-ledger.csv
  prompt-cases/
```
