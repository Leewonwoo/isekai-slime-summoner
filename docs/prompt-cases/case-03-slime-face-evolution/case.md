# 사례 3: 슬라임 기본형 얼굴 제거와 성급별 표정 성장

- 날짜: 2026-07-14
- 도구: ChatGPT 이미지 생성
- 목표: 기본 슬라임은 단순한 실루엣으로 시작하고, ★2·★3에서 얼굴이 생기는 성장 연출 설계
- 연결 에셋: `unit_punch_slime.png`

## v1 — 개선 전

- 프롬프트: 기본 무속성 주먹 슬라임, 귀여운 16-bit 픽셀 아트, 작은 눈과 결연한 미소, 한쪽 주먹을 든 포즈, 평면 초록 크로마키 배경.
- 결과: `v1.png`
- 문제점: 기본 등급부터 눈과 입이 있어 등급 상승에 따른 시각적 성장 여지가 약했다.

## v2 — 개선

- 바꾼 것: 기존 몸체·포즈·색·실루엣은 유지하고 눈·입·모든 얼굴 요소만 제거했다.
- 사용 기법: **image-to-image edit**, **negative prompt**, **invariant locking(변경 불변 요소 고정)**
- 프롬프트 핵심: `Remove the eyes, mouth, and all facial features completely. The base-rank Punch Slime must be a simple featureless jelly blob. Preserve the exact body shape, raised fist pose, palette, pixel-art rendering, outline, framing, and chroma-key background.`
- 결과: `v2.png`

## v3 — 몸체와 공격 표현 분리

- 바꾼 것: 기본 몸체에서 팔과 주먹까지 제거하고, 단순한 얼굴 없는 슬라임 실루엣만 남겼다. 공격 주먹은 별도 스프라이트를 DOTween으로 움직이는 방식으로 분리한다.
- 사용 기법: **image-to-image edit**, **negative prompt**, **scope constraint(프레임 애니메이션 금지 규칙 반영)**
- 프롬프트 핵심: `Remove both arms, fists, and all protruding punch shapes. Leave only one simple smooth rounded slime body. The base body must have no face, no eyes, no mouth, no limbs.`
- 결과: `v3.png`

## 교훈

- 기본형은 얼굴·사지 없는 실루엣으로 단순화하고, ★2부터 눈·입을 추가하면 머지의 성장 보상이 이미지에서도 즉시 읽힌다. 공격 애니메이션은 별도 스프라이트와 코드 연출로 구현한다.
