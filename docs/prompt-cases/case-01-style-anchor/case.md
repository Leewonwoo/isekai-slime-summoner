# 사례 1: 스타일 앵커 피벗 — SD 셀셰이딩 → 척박한 숲 픽셀 아트

- 날짜: 2026-07-11
- 도구: ChatGPT 이미지 (프롬프트 설계는 Claude Code와 협업)
- 목표: 전 에셋(유닛/슬라임/배경/UI 아이콘) 공통 스타일 앵커(레이어 1) 확정 + 배경 4종 프롬프트

## v1 — 실패 (방향 불일치)

- 프롬프트 (스타일 앵커):
  ```
  cute chibi fantasy game asset, big head small body, thick dark outline,
  flat cel shading, solid green background (#00FF00), centered
  ```
- 배경 프롬프트도 같은 앵커 기반: "lush green grass meadow ... vibrant saturated colors" (밝은 초원 십자맵)
- 결과: 이미지 생성 전, 레퍼런스 대조 단계에서 기각
- 문제점: 텍스트로만 앵커를 설계 → 실제 지향점(픽셀 방치형 디펜스의 "척박한 숲 개간지" 무드)과 톤이 어긋남. 셀셰이딩 SD + 화사한 초원은 원하는 황량함·레트로 감성과 정반대. **앵커는 전 에셋에 전파되므로 첫 생성 전에 잡아야 하는 결함**이었음

## v2 — 개선

- 바꾼 것: ① 렌더 스타일을 `flat cel shading` → `16-bit pixel art, retro RPG style`로 교체 ② 무드 키워드 추가 (`muted earthy palette`, `desolate atmosphere`) ③ 배경 소재를 초원 → 마른 풀·죽은 나무·갈라진 흙으로 전면 교체 ④ 카테고리 변수에 배경/UI 프레임(9-slice) 신설
- 사용 기법: **레퍼런스 기반 스타일 그라운딩** (실제 게임 스크린샷을 기준으로 앵커 역설계), **스타일 앵커 고정** (레이어 1을 전 에셋 공통 접두로 유지), **negative prompt** (`no characters, no text, no UI` — 배경에 캐릭터/글자 혼입 방지)
- 프롬프트 (v2 앵커):
  ```
  cute chibi pixel art game asset, 16-bit retro RPG style,
  big head small body, 1px dark outline, muted earthy palette,
  solid green background (#00FF00), centered
  ```
  전체 카테고리 변수는 SPEC §5.1 참조
- 결과: (배경 4종 + UI 프레임 2종 생성 후 이미지 첨부 예정 — asset-ledger.csv 연결)

## 교훈

- 스타일 앵커는 텍스트 상상만으로 정하지 말 것 — **레퍼런스 이미지를 먼저 확정하고 그로부터 앵커를 역설계**해야 전 에셋 재생성 리스크를 없앤다. 앵커 변경 비용은 생성 에셋 수에 비례하므로 0장일 때 잡은 것이 최선.
