# 전 고블린 공용 사망 소멸 이펙트

- 용도: `effect_goblin_death_sheet.png`, Unity 월드 VFX
- 규격: 3×3, 좌→우·상→하 9프레임, 셀당 128×128px, 중심 피벗, 18 FPS 비반복
- 원칙: 고블린 본체·실루엣·무기·속성 문양을 포함하지 않아 모든 고블린과 보스에 공용 적용

## 최종 생성 프롬프트

```text
Use case: stylized-concept
Asset type: reusable Unity 2D enemy-death VFX animation sprite sheet
Input images: the goblin is palette reference only; the existing eruption sheet is layout and pixel-effect readability reference only.
Primary request: exactly nine sequential frames of a character-independent magical disappearance, read left-to-right then top-to-bottom: compact pale-gold impact spark, small moss-green/ochre pulse, expanding dark-brown and olive pixel shards, peak radial burst around an empty middle, then fragments drift upward/outward and shrink until frame 9 is nearly empty.
Scene/backdrop: perfectly uniform solid #FF00FF chroma-key background; no grid, border, shadow, gradient, texture, or floor.
Style/medium: crisp 16-bit pixel art, hard square pixels, restrained olive/moss/dark-brown/ochre/pale-gold palette, no antialiasing or blur.
Composition/framing: strict 3×3 equal cells, identical center anchor, generous padding, no overlap.
Constraints: effect only; absolutely no anatomy, equipment, creature silhouette, corpse, blood, gore, fire, ice, slime, smoke cloud, text, or watermark.
```

원본은 `ArtSource/enemies/`, 크로마 제거·최근접 384×384px 가공본은 `Assets/Art/Enemies/`에 보관한다.
