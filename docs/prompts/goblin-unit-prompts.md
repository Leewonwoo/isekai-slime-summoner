# 아군 고블린 용병단 스프라이트 프롬프트

> SPEC §2.3의 아군 고블린 8종을 정면 단일 스프라이트로 제작한다. 종족 얼굴과 체형은 공통으로 유지하고, 무기와 복장 실루엣으로 역할을 구분한다.

## 1. 공통 규격

- 정면 단일 전신, 캐릭터 시트·후면·측면 이미지 없음
- 64×64급 정보량으로 설계한 뒤 128×128px로 최근접 확대
- 불투명 색상 최대 15색, 하드 알파, 굵은 픽셀 덩어리와 2단 명암
- 완성본은 Point / Compression None / Mipmap Off / PPU 220
- 고블린은 소환사보다 작게 표시하고, 성급 차이는 별·틴트·코드 연출로 처리

## 2. 고블린 궁수

| 원본 | 게임용 결과 | 역할 |
|---|---|---|
| `ArtSource/units/unit_goblin_archer.png` | `Assets/Art/Units/unit_goblin_archer.png` | 무속성 원거리 단일 투사체 |

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense allied goblin archer sprite, front-facing single sprite
Input images: Image 1 is the strict goblin-family identity, proportions, face, palette, pixel-density, and outline reference. Image 2 is the strict simplicity and low-information-density reference.
Primary request: Create one allied Goblin Archer with the same short stocky olive-green body, triangular ears, large simple head, tiny amber eyes, tiny cream lower tusks, and mitten-like hands and feet. Give it one oversized crude wooden bow held diagonally across the torso, an ochre-brown shoulder cowl, and only a small partial quiver behind one shoulder. The bow is the dominant role cue.
Scene/backdrop: perfectly flat uniform #FF00FF chroma-key background; no ground, shadow, gradient, texture, border, reflection, or scenery.
Style/medium: intentionally simple 16-bit retro sprite; 64×64 information density; chunky square pixel clusters; maximum 15 character colors; one-pixel dark outline; flat two-step shading.
Composition/framing: exactly one full-body character, straight front view, centered square canvas, ears, bow, and feet fully visible, generous padding.
Constraints: allied mercenary readability; preserve the goblin-family face and proportions; readable at 48–64px; no text, UI, watermark, or cast shadow.
Avoid: character sheet, multiple characters, other views, club, sword, shield, crossbow, heavy armor, microtexture, gradients, antialiasing, dithering, painterly rendering, glossy 3D, anime style.
```

## 3. 고블린 화염술사

| 원본 | 게임용 결과 | 역할 |
|---|---|---|
| `ArtSource/units/unit_goblin_fire_mage.png` | `Assets/Art/Units/unit_goblin_fire_mage.png` | 화염 속성 범위 스플래시 |

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense allied Goblin Fire Mage sprite, front-facing single sprite
Input images: Image 1 is the strict allied-goblin production reference for face, proportions, outline, pixel density, padding, and detail. Image 2 is the additional goblin identity reference. Image 3 is the strict simplicity ceiling.
Primary request: Create one allied Goblin Fire Mage with the same short stocky olive-green body, oversized simple head, triangular ears, tiny amber eyes, tiny cream lower tusks, and mitten-like hands and feet. Give it one short crooked dark-wood staff held upright, topped with a compact orange-red flame or ember. Add a simple deep-red pointed cloth hood or short shoulder mantle with both ears exposed.
Scene/backdrop: perfectly flat uniform #FF00FF chroma-key background; no ground, shadow, gradient, texture, smoke, particles, reflection, or scenery.
Style/medium: intentionally simple 16-bit retro sprite; 64×64 information density; chunky square pixel clusters; maximum 15 character colors; one-pixel dark outline; flat two-step shading.
Composition/framing: exactly one full-body character, straight front view, centered square canvas, ears, staff tip, and feet fully visible, generous padding.
Constraints: allied mercenary readability; preserve goblin-family face and proportions; flame smaller than the head; no text, UI, watermark, glow, or extra effects.
Avoid: character sheet, multiple characters, other views, bow, club, sword, shield, heavy armor, giant hat, long robe, floating fireball, large flames, microtexture, gradients, antialiasing, dithering, painterly rendering, glossy 3D, anime style.
```

## 4. 후처리

- 마젠타 크로마를 제거하고 알파를 0/255로 정리한다.
- 불투명 영역을 최대 52×52px에 맞춰 64×64 투명 캔버스 중앙에 배치한다.
- 불투명 색상을 최대 15색으로 양자화하고 128×128px로 2배 최근접 확대한다.
