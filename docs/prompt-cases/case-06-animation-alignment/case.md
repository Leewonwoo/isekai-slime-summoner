# 사례 6: 캐릭터 애니메이션 위치·크기 흔들림 개선

- 날짜: 2026-07-17
- 도구: ChatGPT 이미지 생성 프롬프트 / Codex 문서화
- 목표: 소환사·슬라임·고블린 9프레임 시트에서 캐릭터 위치와 크기가 프레임마다 달라지는 현상을 공통 규칙으로 방지

## v1 — 문제

- 결과: `v1-idle.png`, `v1-attack.png`
- 프롬프트 문제: 동일 캐릭터·동일 크기·중앙 배치만 요구했지만, 무엇을 기준으로 정렬하고 크기를 계산할지 정의하지 않았다.
- 생성 문제: 비대칭 지팡이와 포즈별 외곽선이 달라질 때 프레임마다 전체 실루엣이 따로 중앙 정렬되어 몸체가 좌우·상하로 밀리거나 크기가 달라질 수 있었다.
- 파이프라인 위험: 시트를 프레임별 알파 바운딩으로 자동 크롭하면 생성 단계의 작은 오차가 Unity 재생에서 큰 흔들림으로 확대된다.

## v2 — 공통 마스터 프롬프트

- 변경: Image 1 정체성 → Image 2 승인 정렬 → Image 3 동작 참고의 우선순위를 정의했다.
- 변경: 소환사·고블린은 발 중앙, 슬라임은 몸체 밑면 중앙을 셀 로컬 `(64, 112)`에 두는 루트 앵커 프로필을 분리했다.
- 변경: 지팡이·몽둥이·주먹·분사·역할 장식 등 변화하는 외곽을 정렬·크기 계산에서 제외했다.
- 변경: 384×384 전체 시트, 128×128 고정 셀, 동일 Center 피벗, 캐릭터별 동일 PPU, 프레임별 자동 크롭 금지를 후처리 계약으로 묶었다.
- 사용 기법명: reference hierarchy, identity preservation, invariant locking, root-anchor normalization, negative prompting, fixed-grid post-processing.
- 프롬프트: [animation-master-prompt.md](../../prompts/animation-master-prompt.md)
- 결과: `v2-idle.png`, `v2-attack.png`. 공통 마스터로 재생성한 뒤 고정 셀 내부의 몸체 루트를 정규화해 프로젝트 파일을 교체했다.

## v2 검증

- idle·attack 모두 384×384 RGBA, 128×128 고정 셀 9개다.
- 18개 프레임의 몸체 앵커가 셀 로컬 `x=63.5~64.5`, `y=112`에 들어온다.
- 셀 경계에 닿거나 잘린 프레임이 없으며 Unity의 기존 9개 Sprite Rect·Center 피벗·PPU 140 메타를 유지한다.
- 적용 에셋: `Assets/Art/MAIN_SUMMONER_idle_sheet.png`, `Assets/Art/MAIN_SUMMONER_attack_sheet.png`.
- 후처리 도구: `tools/normalize_animation_sheet.py`.

## 교훈

- “같은 크기로 중앙 배치”보다 **변하지 않는 몸체 루트를 좌표로 고정하고, 무기와 임시 공격부위를 정렬 계산에서 제외**해야 애니메이션 흔들림을 줄일 수 있다.
- 이미지 생성 좌표 지시는 보조 수단이며, 최종 품질은 프레임별 크롭 없는 고정 셀 슬라이스와 동일 피벗 검수로 보장한다.
