# CrossLoad Defense — 십자 디펜스 (Cross Defense)

NAN 2026 (NHN Game × AI Hackathon) 사전 과제. 1인 개발, 마감 **2026-08-10**.

**전체 스펙(단일 기준 문서): [docs/SPEC.md](docs/SPEC.md)** — 게임 설계·일정·제출물 관련 판단은 반드시 이 문서를 따른다.

## 프로젝트 개요

십자형 타워 디펜스 × 로그라이크 × 키우기. 중앙 코어 + 4방향 레인(레인당 슬롯 3칸), 유닛 소환·3합성(오토체스식)·재배치. 웨이브 20개 + 보스. 차별화 요소: LLM 웨이브 디렉터(플레이 데이터 기반으로 다음 웨이브를 AI가 설계).

## 기술 스택 / 환경

- Unity **6000.3.5f2**, 2D URP 템플릿, Input System
- 빌드 타깃: **Android APK (모바일 빌드)** — 2026-07-11 사용자 결정으로 WebGL에서 변경
  - 세로 고정 (Portrait, 1080×1920 기준)
  - ⚠️ SPEC §7의 원래 제출 요건은 "링크 클릭만으로 브라우저 플레이"였음 — 접수 요강에서 APK 허용 여부 확인 필요
- UI: **UI Toolkit** (HUD/하단 패널/배지) + 월드 오브젝트 SpriteRenderer (필드/유닛/몹/투사체)
  - 필드를 UI Toolkit으로 그리지 말 것
- 연출: 프레임 애니메이션 금지, 정적 스프라이트 + DOTween 코드 연출
- 데이터: ScriptableObject (웨이브/유닛/강화), 오브젝트 풀링 (몹/투사체)
- LLM 호출: 클라이언트 직접 호출 금지 → Cloudflare Workers 프록시, 실패 시 로컬 규칙 폴백 필수

## 작업 규칙

- **커밋 메시지에 AI 활용 흔적 남기기** (예: `feat: AI-generated unit sprites integrated`) — 커밋 기록 자체가 제출물 증거
- AI 생성 에셋을 추가할 때마다 [docs/asset-ledger.csv](docs/asset-ledger.csv)에 기록 (파일명/도구/프롬프트/라이선스)
- 프롬프트 개선 사례(v1→v2)는 `docs/prompt-cases/`에 보존
- 스코프 아웃 항목(SPEC §8) 추가 금지. 고민되면 자르는 쪽으로
- 4주차(8/4~)는 기능 동결 — 폴리싱/밸런싱/문서만

## 디렉터리 구조 (계획)

```
Assets/
  Scripts/        # 게임 코드 (Core, Units, Waves, UI, Meta, LLM)
  Data/           # ScriptableObject 에셋
  Sprites/        # AI 생성 스프라이트
  UI/             # UXML/USS, PanelSettings
  Scenes/
docs/
  SPEC.md         # 단일 기준 문서
  asset-ledger.csv
  prompt-cases/
```
