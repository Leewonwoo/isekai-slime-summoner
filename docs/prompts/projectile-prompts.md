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

## 1.1 2026-07-19 전체 투사체 세트

공통 생성 도구: `ChatGPT image_gen`

공통 용도: Unity 2D portrait mobile tower-defense world projectile

공통 스타일: strict chunky 16-bit pixel art, hard square pixels, limited palette, fully opaque clusters, dark outline, no blur/antialiasing/soft alpha, right-facing side view, no text/UI/watermark.

### 소환사 단일 투사체

각 이미지는 기존 `projectile_energy_bolt.png`, `projectile_fireball.png`, `projectile_iceball.png`를 스타일 참조로만 사용했다.

| 파일 | 최종 프롬프트 핵심 |
|---|---|
| `projectile_summoner_arcane_bolt.png` | one compact violet-white arcane bolt; bright lavender core, tapered right nose, short jagged left tail, two square sparks; `#00FF00` chroma key |
| `projectile_summoner_fireball.png` | one compressed fireball; pale-yellow core, orange-red shell, sharp right nose, short jagged left flame tail; `#00FF00` chroma key |
| `projectile_summoner_ice_lance.png` | one slender angular ice spear; icy-white ridge, cyan right point, swept cobalt fins and tiny trailing frost shards; `#00FF00` chroma key |
| `projectile_summoner_lightning_orb.png` | one nature lightning orb; pale-yellow core, lime/turquoise shell, right-facing lightning prongs and zigzag left tail; no plant motif; `#FF00FF` chroma key |

### 슬라임 성급별 3열 원본

각 계열은 해당 슬라임의 ★1·★2·★3 유닛 스프라이트를 성급 정체성 참조로 사용했다. 한 정사각형 원본 안에 왼쪽부터 ★1, ★2, ★3 투사체를 정확히 하나씩 배치하고 라벨·구분선·테두리는 넣지 않았다.

| 원본 스트립 | 최종 프롬프트 핵심 |
|---|---|
| `projectile_watergun_slime_rank_strip.png` | water bead → compressed water capsule with two splash fins → dense cannon-water orb with white-blue pressure core and three swept fins; `#00FF00` |
| `projectile_flame_slime_rank_strip.png` | ember pellet → round fireball with yellow core and red shell → white-hot blazing core with crown-like flame tail; `#00FF00` |
| `projectile_ice_slime_rank_strip.png` | faceted frost pebble → broad angular shard → blunt hexagonal glacier crystal; heavy slow family, never a thin lance; `#00FF00` |
| `projectile_green_slime_rank_strip.png` | bright seed pellet → pointed leaf dart → thorned bloom-seed pod with acid-yellow core; `#FF00FF` |
| `projectile_explosion_slime_rank_strip.png` | unstable ember bomb → cracked blast core with four pressure spikes → compressed magma bomb with glowing cracks and six radial spikes; no comet tail; `#00FF00` |
| `projectile_freeze_slime_rank_strip.png` | short ice needle → faceted lance with swept fins → long tri-pronged rail-ice spear; slender piercing family; `#00FF00` |

가공은 크로마키 원본을 `ArtSource/projectiles/`에 보존한 뒤, 가장 큰 3개 연결 픽셀 군집을 X순으로 추출해 `Assets/Art/Projectiles/`의 128×128 RGBA 단일 스프라이트로 분리했다. Unity 임포트는 Point, Mipmap Off, PPU 100, 무압축을 사용한다.

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
