# 적군 고블린 정면 기본 스프라이트 16종

> 2026-07-20 기준 정적 기본 이미지 로스터. 50웨이브의 속성·역할 조합을 위해 8종에서 16종으로 확장했다. 애니메이션 시트·측면·후면·공격 이펙트는 포함하지 않는다.

## 공통 제작 규격

- 기준 이미지: `ArtSource/enemies/enemy_goblin_grunt.png`
- 정확히 한 명, 정면, 전신, 정지 준비 자세
- 단색 `#FF00FF` 크로마키 배경
- 64×64 정보량, 굵은 픽셀 군집, 캐릭터 최대 15색, 2단 명암
- 가공본: 투명 RGBA 128×128px, Point, Compression None, Mipmap Off, PPU 220
- 공통 정체성: 올리브색 피부, 긴 삼각 귀, 작은 호박색 눈, 아래 엄니 2개
- 금지: 캐릭터 시트, 다른 방향, 그림자, 지면, 실제 속성 이펙트, 파티클, 고해상도 질감

## 역할별 프롬프트 델타

| 파일 | 역할 | 공통 프롬프트에 추가하는 외형 |
|---|---|---|
| `enemy_goblin_grunt.png` | 일반병 | 누더기 갈색 허리천, 단순 벨트, 한 손 나무 몽둥이 |
| `enemy_goblin_fire_scout.png` | 화염 정찰병 | 마른 체형, 녹슨 적색 스카프와 불꽃 문양 천, 짧은 단검, 최소 가죽 붕대 |
| `enemy_goblin_ice_bruiser.png` | 빙결 중갑병 | 넓은 체형, 청회색 철모·어깨 보호대, 철테 둥근 방패, 짧고 무거운 몽둥이 |
| `enemy_goblin_nature_raider.png` | 자연 약탈자 | 짙은 숲색 후드·잎 모양 어깨천·이끼색 허리천, 짧은 클리버, 전리품 주머니 |
| `enemy_goblin_chief.png` | 족장 | 가죽 어깨 망토, 뼈·적색 천 머리띠, 넓은 벨트, 굵은 옹이 철퇴 |
| `enemy_goblin_warlord.png` | 화염 대전사 | 무딘 흑철 투구·어깨 갑주, 짙은 적색 전투 천, 주황 철띠가 감긴 큰 전투 몽둥이 |
| `enemy_goblin_slinger.png` | 원거리 투석병 | 갈색 머리띠, 가죽 투석구, 허리 돌주머니와 둥근 돌 3개, 황갈색 허리천 |
| `enemy_goblin_golden.png` | 황금 고블린 | 귀가 드러나는 황금색 후드·조끼, 묶은 동전 주머니, 손에 든 큰 금화 1개, 무기 없음 |
| `enemy_goblin_fire_mage.png` | 화염술사 | 귀가 드러나는 짙은 적색 뾰족 후드, 녹슨 적색 로브 천, 주황 불씨석이 박힌 굽은 지팡이 |
| `enemy_goblin_fire_bomber.png` | 화약 폭탄병 | 녹슨 적색 가죽 모자·가슴 천, 불이 붙지 않은 흑철 폭탄 1개, 폭탄 2개가 일부 보이는 가방 |
| `enemy_goblin_frost_stalker.png` | 서리 추적자 | 마른 체형, 옅은 청색 스카프·손목끈, 짧은 청회색 쌍단검 |
| `enemy_goblin_ice_archer.png` | 빙결 궁수 | 귀가 드러나는 청회색 후드, 옅은 청색 끈이 감긴 단궁, 화살촉 3개가 보이는 작은 화살통 |
| `enemy_goblin_ice_shaman.png` | 얼음 주술사 | 청회색 모피 테두리 두건, 옅은 청색 부적, 청회색 결정석이 박힌 짧은 지팡이 |
| `enemy_goblin_thorn_hunter.png` | 가시 사냥꾼 | 숲색 머리띠, 짧은 목제 취관, 짙은 녹색 가시 다트 3개가 보이는 허리 화살통 |
| `enemy_goblin_bark_guard.png` | 나무껍질 방패병 | 넓은 체형, 굵은 나이테 3줄의 둥근 나무껍질 방패, 짧은 뿌리 몽둥이 |
| `enemy_goblin_spore_shaman.png` | 포자 주술사 | 버섯 조각 2개가 붙은 이끼색 두건, 버섯갓이 달린 가지 지팡이, 작은 약초 주머니 |

## 생성 프롬프트 템플릿

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense enemy sprite, one static front-facing sprite
Input images: Image 1 is the strict species, face, pixel-density, outline, proportion, and production-style anchor. Create a new role variant; do not make a sheet.
Primary request: Create the enemy "{ROLE_NAME}": the same goblin species as Image 1. Keep olive-green skin, long triangular ears, tiny amber eyes, and two tiny lower tusks. Add only: {ROLE_DELTA}.
Scene/backdrop: perfectly flat uniform solid #FF00FF chroma-key background; no ground, shadow, gradient, texture, border, or lighting variation.
Style/medium: authentic simple 16-bit retro game sprite, intentionally low information density, chunky square pixel clusters, maximum 15 character colors, one-pixel dark outline at intended 64×64 information scale, flat two-step shading only. Match Image 1's simplicity.
Composition/framing: exactly one goblin, straight front view, full body centered, neutral ready pose, both feet visible, generous empty padding, square canvas.
Constraints: same face and species identity as Image 1; single subject only; front view only; readable at 48–64 px; no text, UI, watermark, cast shadow, or cropped body parts.
Avoid: character sheet, animation frames, side/back/three-quarter view, actual elemental effects, particles, gradients, dithering, antialiasing, painterly illustration, 3D, scenery.
```

## 후처리

`Tools/process_goblin_variants.py`가 크로마 제거 결과를 64px 논리 캔버스에 맞춰 최대 52px 바운드·하단 4px 여백으로 정렬하고, 하드 알파·최대 15색으로 양자화한 뒤 128px 최근접 확대한다.
