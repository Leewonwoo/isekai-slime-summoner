# ★3 자동 스킬 공용 이펙트 프롬프트

2026-07-19 생성. 기존 `effect_impact_burst_neutral.png`, `projectile_fireball.png`,
`projectile_iceball.png`, `unit_green_slime_star3.png`은 스타일·팔레트 참조로만 사용했다.

## 공통 규격

```text
Use case: stylized-concept
Asset type: Unity 2D world-space static skill effect sprite for a portrait mobile tower-defense game
Input images: reference images only; match their chunky strict 16-bit pixel-art language, hard square pixels, pixel scale, limited palette, and dark outline treatment
Style/medium: strict hand-placed-looking 16-bit pixel art, fully opaque pixel clusters, no blur, no soft transparency
Composition/framing: exactly one centered radial effect, symmetric readable silhouette at 32–64 pixels, generous empty padding, no character
Constraints: hard crisp separated edges; no cast shadow; no text; no UI frame; no watermark
Avoid: smooth vector art, painterly rendering, 3D, motion blur, smoke cloud, translucent aura, multiple effects
```

## 중립 충격파

```text
Primary request: create a powerful neutral-element rank-three ground impact burst for a slime's periodic special skill
Subject: compact ivory-white central shock core, eight blunt lavender impact rays, two broken concentric pressure rings, a few square debris pixels; heavier and wider than the reference neutral burst, not a spell badge
Color palette: warm ivory, pale lavender, saturated violet, dark purple-brown outline
Scene/backdrop: perfectly flat uniform solid #00FF00 chroma-key background
```

## 화염 노바

```text
Primary request: create a powerful fire-element rank-three nova impact for periodic flame and explosion slime skills
Subject: white-hot circular blast core, jagged orange-red outward flame crown, broken ring of compact ember blocks; radial explosion with no directional comet tail
Color palette: pale yellow-white, gold, saturated orange, red-orange, deep burgundy outline
Scene/backdrop: perfectly flat uniform solid #00FF00 chroma-key background
```

## 빙결 결정파

```text
Primary request: create a powerful ice-element rank-three crystal burst for periodic ice and freeze slime skills
Subject: bright icy central diamond, four long cardinal crystal spikes, four shorter diagonal shards, broken angular frost ring; sharp radial impact
Color palette: icy white, pale cyan, saturated sky blue, cobalt, dark navy outline
Scene/backdrop: perfectly flat uniform solid #00FF00 chroma-key background
```

## 자연 개화

```text
Primary request: create a powerful nature-element rank-three bloom burst for periodic green and support slime skills
Subject: bright lime seed core opening into four broad leaf-shaped energy petals, a broken circular vine ring, several small seed pixels; magical botanical burst without a flower stem or scenery
Color palette: pale yellow-green, lime, emerald, teal, very dark forest outline; no magenta
Scene/backdrop: perfectly flat uniform solid #FF00FF chroma-key background
```
