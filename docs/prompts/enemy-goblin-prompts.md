# 적군 고블린 스프라이트 생성 계획 — 16-bit 고블린 숲

> 2026-07-14 요청 기준으로 슬라임 아군과 대치하는 고블린 적군의 첫 스타일 앵커를 만든다.
> 적 고블린은 이동 방향별 뷰를 만들지 않고 정면 단일 스프라이트만 사용한다.

## 1. 생성 원칙

- 모든 적 고블린은 정면 단일 스프라이트로 생성한다.
- 방향 표현은 별도 이미지가 아니라 이동·흔들림·스쿼시 연출로 처리한다.
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

## 3. 후처리·임포트 규격

- 마젠타 키 제거: border auto-key + soft matte + despill.
- 알파를 하드 컷한 뒤 64×64 캔버스에 배치한다.
- 불투명 영역은 디더링 없이 최대 15색으로 양자화한다.
- 64×64 결과를 최근접 보간으로 128×128 확대한다.
- Unity: Sprite (2D and UI), Point, Compression None, PPU 220, Mipmap Off. 캐릭터 기본값(PPU 200)보다 약 9% 작게 표시해 소환사보다 작은 월드 바운드를 유지한다.
