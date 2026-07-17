# 적군 고블린 스프라이트 생성 계획 — 16-bit 고블린 숲

> 2026-07-14 요청 기준으로 슬라임 아군과 대치하는 고블린 적군의 첫 스타일 앵커를 만든다.
> 적 고블린은 이동 방향별 뷰를 만들지 않고 정면 이미지만 사용한다. 실제 이동 일반병은 정면 달리기 9프레임 시트를 사용한다.

## 1. 생성 원칙

> 모든 달리기·공격 시트는 [캐릭터 애니메이션 공통 마스터 프롬프트](animation-master-prompt.md)의 `HUMANOID` 프로필을 사용한다. 발 중앙을 루트로 고정하고 몽둥이·귀·팔은 정렬 및 크기 계산에서 제외한다. 이 문서의 `#FF00FF` 배경 규칙이 마스터의 `[CHROMA_KEY_HEX]` 값이다.

- 모든 적 고블린은 정면 카메라로 생성하고 후면·측면 이미지는 만들지 않는다.
- 기본 방향 표현은 이동·흔들림·스쿼시로 처리하되, 실제 이동 일반병은 정면 달리기 9프레임을 재생한다.
- 아군 `unit_punch_slime.png`와 나란히 놓았을 때 이질감이 없도록 64px급 정보량과 굵은 픽셀 덩어리를 기준으로 한다.
- 생성 원본의 미세 색 변화는 후처리에서 15색 이하로 양자화한다.
- 고속·탱크·특수·황금 고블린도 같은 정면 단일 규격으로 확장한다.

## 2. 기본 적 고블린

| 원본 | 게임용 결과 | 역할 |
|---|---|---|
| `ArtSource/enemies/enemy_goblin_grunt.png` | `Assets/Art/Enemies/enemy_goblin_grunt.png` | 표준 HP·속도의 일반 근접 적 |

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense enemy sprite, front-facing single sprite
Input images: Image 1 is a subject reference for the basic goblin's identity only. Image 2 is the strict target reference for pixel density, simple shading, chunky silhouette, and in-game visual complexity. Create a new sprite; do not make a sheet.
Primary request: Redesign the basic enemy goblin as one much simpler, lower-detail front-facing sprite that belongs beside Image 2 in the same game. Keep only the essential goblin idea: short stocky olive-green goblin, pointed ears, small lower tusks, ragged brown loincloth, simple belt, and one crude wooden club.
Scene/backdrop: perfectly flat uniform solid #FF00FF chroma-key background. No ground plane, shadow, gradient, texture, border, or lighting variation.
Style/medium: authentic simple 16-bit retro game sprite with intentionally low information density; chunky square pixel clusters; very limited 12–16 color palette; one-pixel dark outline at intended 64×64 sprite scale; flat two-step shading only. Match Image 2's simplicity. No micro-texture, no skin pores, no fabric grain, no dithering, no painterly highlights, no high-resolution illustration detail.
Composition/framing: exactly one goblin, straight front view, full body centered, neutral idle pose, both feet visible, club held upright at one side, generous empty padding, square canvas. No other views and no animation frames.
Character simplification: large simple head, small body, ears as clean triangular shapes, eyes as two tiny amber pixel marks, tusks as two tiny cream pixel marks, hands and feet as simplified mitten-like shapes. Club must be a single readable brown silhouette with at most one highlight band.
Lighting/mood: flat sprite lighting; cute hostile grunt; readable at 48–64 px in a mobile battle.
Color palette: muted olive green, dark brown, tan, charcoal outline, tiny amber eyes, tiny cream tusks. Do not use #FF00FF or near-magenta in the goblin.
Constraints: replace the over-detailed look with a visibly simpler game sprite; single subject only; front view only; strong silhouette; no text; no UI; no watermark; no cast shadow; no cropped ears, club, or feet.
Avoid: character sheet, back view, side view, three-quarter view, realistic anatomy, detailed muscles, detailed wrinkles, detailed face rendering, gradients, smooth anti-aliased illustration, high-resolution texture, glossy 3D, anime art, armor, shield, scenery, floor, reflection, logo.
```

## 3. 일반병 정면 달리기 9프레임

| 원본 | 게임용 결과 | 역할 |
|---|---|---|
| `ArtSource/enemies/enemy_goblin_grunt_run_sheet.png` | `Assets/Art/Enemies/enemy_goblin_grunt_run_sheet.png` | 런타임 이동 중 재생하는 정면 달리기 루프 |

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense enemy goblin 9-frame run-animation sprite sheet
Input images: Image 1 is the strict identity and style reference. Preserve the same face, proportions, palette, loincloth, belt, club hand, outline, and front-facing camera.
Primary request: Create a strict 3×3 sheet with exactly nine consecutive frames of the same goblin running forward toward the viewer. Order frames left-to-right, top-to-bottom: left contact, compression, left passing, rise, right contact, compression, right passing, rise, transition back to frame 1. The club bobs but remains in the same hand.
Scene/backdrop: one continuous perfectly flat #FF00FF chroma-key background; no grid lines, labels, numbers, ground, shadows, gradients, texture, effects, or scenery.
Style/medium: intentionally simple 16-bit sprite; each cell at 64×64 information density; chunky pixels; maximum 15 shared character colors; one-pixel dark outline; flat two-step shading.
Composition/framing: exact equal 3×3 layout, one centered full-body goblin in every cell, consistent scale and baseline, all body parts and club inside each cell.
Constraints: exactly nine frames; same identity and camera throughout; only limbs, club bob, and subtle body squash/rise change; no text, UI, watermark, separators, dust, or motion trails.
Avoid: extra or missing frames, turnarounds, side/back/three-quarter views, swapped club hand, redesigned clothing, extra weapons, armor, gradients, dithering, antialiasing, painterly or 3D rendering.
```

## 4. 일반병 몽둥이 공격 9프레임

| 원본 | 게임용 결과 | 역할 |
|---|---|---|
| `ArtSource/enemies/enemy_goblin_grunt_attack_sheet.png` | `Assets/Art/Enemies/enemy_goblin_grunt_attack_sheet.png` | 근접 공격 준비·내려치기·타격·복귀 |

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense enemy goblin 9-frame melee attack-animation sprite sheet
Input images: Image 1 is the strict identity reference. Image 2 is the strict 3×3 production-layout and cross-frame consistency reference. Preserve the same face, proportions, palette, loincloth, belt, club shape, club hand, and front-facing camera.
Primary request: Create one heavy in-place club strike in exactly nine frames: neutral ready, anticipation, strong wind-up, peak raised pose, diagonal downswing, low impact, recoil, recovery, near-idle. Frame 6 is the clearest impact keyframe and frame 9 returns naturally to frame 1.
Scene/backdrop: one continuous perfectly flat #FF00FF chroma-key background; no grid lines, labels, numbers, ground, shadows, gradients, texture, effects, target, or scenery.
Style/medium: intentionally simple 16-bit sprite; each cell at 64×64 information density; chunky pixels; maximum 15 shared character colors; one-pixel dark outline; flat two-step shading.
Composition/framing: exact equal 3×3 layout, one centered full-body goblin in every cell, consistent scale and foot baseline, all body parts and raised club inside each cell.
Constraints: exactly nine frames; club never switches hands; same identity and front camera throughout; only pose, club angle, limb placement, and compact squash change; no text, UI, watermark, separators, target, dust, burst, or motion trail.
Avoid: extra or missing frames, duplicated poses, turnarounds, side/back/three-quarter views, swapped club hand, extra weapons, armor, gradients, dithering, antialiasing, painterly or 3D rendering.
```

## 5. 후처리·임포트 규격

- 마젠타 키 제거: border auto-key + soft matte + despill.
- 알파를 하드 컷한 뒤 64×64 캔버스에 배치한다.
- 불투명 영역은 디더링 없이 최대 15색으로 양자화한다.
- 64×64 결과를 최근접 보간으로 128×128 확대한다.
- Unity: Sprite (2D and UI), Point, Compression None, PPU 220, Mipmap Off. 캐릭터 기본값(PPU 200)보다 약 9% 작게 표시해 소환사보다 작은 월드 바운드를 유지한다.
- 달리기·공격 시트는 384×384px, 프레임당 128×128px의 정규 3×3 그리드와 동일 Center 피벗으로 슬라이스한다. 프레임별 알파 자동 크롭은 금지한다.
