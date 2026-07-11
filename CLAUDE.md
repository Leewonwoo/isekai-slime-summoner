# CrossLoad Defense — 십자 디펜스 (Cross Defense)

NAN 2026 (NHN Game × AI Hackathon) 사전 과제. 1인 개발, 마감 **2026-08-10**.

**전체 스펙(단일 기준 문서): [docs/SPEC.md](docs/SPEC.md)** — 게임 설계·일정·제출물 관련 판단은 반드시 이 문서를 따른다.

## 프로젝트 개요

십자형 타워 디펜스 × 로그라이크 × 키우기. 중앙 코어 + 4방향 레인(레인당 슬롯 3칸), 유닛 소환·3합성(오토체스식)·재배치. 웨이브 20개 + 보스. 웨이브는 **규칙 기반**(SO 웨이브 테이블 + 방향 가중치 + 랜덤 시드 변주) — **런타임 LLM API 호출은 스코프 아웃**, AI는 개발 도구(코드·에셋·밸런싱 CSV 생성)로만 사용.

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
- 프롬프트 개선 사례(v1→v2)는 `docs/prompt-cases/`에 보존
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
