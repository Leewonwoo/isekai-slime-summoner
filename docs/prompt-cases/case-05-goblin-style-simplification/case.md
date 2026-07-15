# 사례 5: 적 고블린 3뷰 폐기와 픽셀 밀도 단순화

- 날짜: 2026-07-14
- 도구: Codex 내장 이미지 생성
- 목표: 과도하게 정교한 3뷰 고블린 시트를 프로젝트의 단순한 픽셀 스타일에 맞는 정면 단일 스프라이트로 교체

## v1 문제

- 결과: `v1.png`
- 문제: 정면·후면·측면 3뷰는 현재 게임에 불필요했고, 피부·근육·의상·무기 질감이 아군 주먹 슬라임보다 지나치게 정교했다.
- 영향: 작은 인게임 크기에서 다른 프로젝트의 에셋처럼 보이고, 향후 적 종류 생성 비용도 불필요하게 증가한다.

## v2 개선

- 결과: `v2.png`
- 변경: 정면 단일 구도로 제한하고 얼굴·손발·의상·곤봉을 필수 형태만 남도록 단순화했다.
- 사용 기법명: reference-image conditioning, constraint reduction, negative prompt, palette quantization, nearest-neighbor downscaling.
- 스타일 기준: `Assets/Art/Units/unit_punch_slime.png`의 픽셀 밀도와 단순한 실루엣.
- 후처리: 마젠타 제거 → 64×64 배치 → 하드 알파 → 최대 15색 무디더 양자화 → 128×128 최근접 확대.

## 검증

- 정면 캐릭터 1개만 존재한다.
- 최종 불투명 색상은 15개다.
- 최종 파일은 128×128이며 Point/무압축/PPU 220/Mipmap Off로 임포트한다. 캐릭터 기본값 200에서 체형 보정한 값이다.
- 적용 에셋: `Assets/Art/Enemies/enemy_goblin_grunt.png`.
