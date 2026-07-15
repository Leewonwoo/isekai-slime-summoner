# 소환사 기본 투사체 프롬프트

2026-07-14 생성. `ArtSource/units/MAIN_SUMMONER.png`을 스타일 레퍼런스로 사용했으며,
각 투사체는 별도 `Codex image_gen` 호출로 제작했다.

## 1. 에너지 볼트

```text
Use case: stylized-concept
Asset type: Unity 2D world projectile sprite for a portrait mobile tower-defense game
Primary request: create one neutral-element energy bolt fired by the human summoner, traveling horizontally to the right
Input images: Image 1 is a style reference only; match its chunky strict 16-bit pixel-art language, pixel scale, dark outline treatment, and violet magic identity
Subject: a compact round violet energy core with a bright white-lavender center, a short tapered comet-like magic tail pointing left, and two or three crisp electric sparks; strong readable silhouette at 32–64 pixels; clearly different from flame or ice
Style/medium: strict hand-placed-looking 16-bit pixel art, hard square pixels, limited palette, fully opaque pixel clusters, no blur, no soft transparency
Composition/framing: single projectile centered, horizontal side view, nose pointing right, generous empty padding, no character
Color palette: deep violet, saturated purple, lavender, tiny white highlight, very dark purple-brown outline; absolutely no green in the projectile
Scene/backdrop: perfectly flat uniform solid #00FF00 chroma-key background
Constraints: background must have no gradient, texture, lighting variation, floor, shadow, reflection, or glow spill; crisp separated edges; no cast shadow; no text; no UI frame; no watermark
Avoid: smooth vector art, painterly rendering, 3D render, circular badge, weapon, character, multiple projectiles, motion blur, smoke, translucent aura
```

## 2. 파이어볼

```text
Use case: stylized-concept
Asset type: Unity 2D world projectile sprite for a portrait mobile tower-defense game
Primary request: create one fireball fired by the human summoner, traveling horizontally to the right
Input images: Image 1 is a style reference only; match its chunky strict 16-bit pixel-art language, pixel scale, and dark outline treatment
Subject: a compact round blazing fire core with a bright pale-yellow center, orange-red outer flame, and a short jagged flame tail streaming left; strong readable silhouette at 32–64 pixels; clearly different from neutral magic and ice
Style/medium: strict hand-placed-looking 16-bit pixel art, hard square pixels, limited palette, fully opaque pixel clusters, no blur, no soft transparency
Composition/framing: single projectile centered, horizontal side view, nose pointing right, generous empty padding, no character
Color palette: pale yellow, gold, saturated orange, red-orange, deep burgundy-brown outline; absolutely no green in the projectile
Scene/backdrop: perfectly flat uniform solid #00FF00 chroma-key background
Constraints: background must have no gradient, texture, lighting variation, floor, shadow, reflection, or glow spill; crisp separated edges; no cast shadow; no text; no UI frame; no watermark
Avoid: smooth vector art, painterly rendering, 3D render, circular badge, weapon, character, multiple projectiles, motion blur, smoke cloud, translucent aura
```

## 3. 아이스볼

```text
Use case: stylized-concept
Asset type: Unity 2D world projectile sprite for a portrait mobile tower-defense game
Primary request: create one ice ball fired by the human summoner, traveling horizontally to the right
Input images: Image 1 is a style reference only; match its chunky strict 16-bit pixel-art language, pixel scale, and dark outline treatment
Subject: a compact faceted frozen orb with a bright icy-white center, angular cyan-blue crystal shell, a pointed frost nose facing right, and a short trail of two or three sharp ice shards streaming left; strong readable silhouette at 32–64 pixels; clearly different from fire and neutral magic
Style/medium: strict hand-placed-looking 16-bit pixel art, hard square pixels, limited palette, fully opaque pixel clusters, no blur, no soft transparency
Composition/framing: single projectile centered, horizontal side view, nose pointing right, generous empty padding, no character
Color palette: icy white, pale cyan, saturated sky blue, deep cobalt, very dark navy outline; absolutely no green in the projectile
Scene/backdrop: perfectly flat uniform solid #00FF00 chroma-key background
Constraints: background must have no gradient, texture, lighting variation, floor, shadow, reflection, or glow spill; crisp separated edges; no cast shadow; no text; no UI frame; no watermark
Avoid: smooth vector art, painterly rendering, 3D render, circular badge, snowflake emblem, weapon, character, multiple projectiles, motion blur, mist, smoke, translucent aura
```

