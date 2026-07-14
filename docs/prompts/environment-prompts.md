# 인게임 배경(환경) 프롬프트 팩 — 고블린 숲·무경로 전장 v3

> SPEC §5.1 3단 아키텍처의 **배경/환경 카테고리 실전 전개본**. ChatGPT 이미지 생성에 복붙해서 사용.
> 생성 후: 원본은 `ArtSource/`, 가공본은 `Assets/Art/`, [asset-ledger.csv](../asset-ledger.csv)에 기록.
> 실패→개선을 겪으면 그날 [prompt-cases/](../prompt-cases/README.md)에 사례 추가.

## 0. 공통 규격

- **스타일 앵커(환경용)** — 모든 프롬프트 끝에 이미 포함돼 있음. 수정 시 SPEC §5.1 먼저 개정:
  ```
  16-bit retro RPG style, clean chunky pixels, muted dark forest palette,
  enclosed natural arena, no characters, no text, no UI
  ```
- **크로마키**: 화면을 가득 채우는 ①만 예외, 나머지는 전부 `solid green background (#00FF00)`
- **후처리(전 에셋 공통)**: 가짜 픽셀 정리 — 최근접 보간으로 1/4~1/8 다운스케일 → 필요 시 재확대. Unity 임포트: Filter **Point**, Compression **None**, PPU 통일 (SPEC §5.1)
- **생성 사이즈**: ① 은 **정사각(1024×1024)** — 필드 존이 1080×960(≈1:1)이라 세로형으로 뽑으면 위아래가 잘려 나감. 나머지는 정사각 기본

## 1. 필드 통바닥 (v3 채택 — 무경로 폐쇄형 고블린 숲)

몬스터가 어느 방향에서든 등장할 수 있는 숲 전장이다. 맵 원본에는 길·레인·슬롯·소환진·포탈·장식 프롭을 그리지 않는다. 유닛·소환사·스폰 위치와 전투용 표시는 Unity가 별도 배치한다.

```
top-down 16-bit pixel art environment background for a mobile survival defense game:
an enclosed goblin forest arena with a continuous natural mossy forest floor,
dense trees, trunks, bushes and tangled roots forming an irregular organic boundary,
the center and most of the frame open and readable for units and monsters,
soft leaf litter and subtle ground texture only, no designed landmarks,
non-symmetrical natural composition, no focal point, square image,
16-bit retro RPG style, clean chunky pixels, muted dark forest palette,
no paths, no trails, no lanes, no cross shape, no grid, no placement pads,
no rune, no portal, no shrine, no hut, no fence, no gate, no torch,
no banner, no totem, no mushroom, no placed rocks, no decorative props,
no characters, no enemies, no projectiles, no text, no UI
```

- 가장자리 숲은 사방을 둘러싸되, 특정 방향의 입구나 출구가 보이지 않아야 함
- 중앙을 비우되 원형 광장처럼 보이게 만들지 말고, 자연스러운 숲 바닥 질감만 유지
- 장식물이 필요하면 배경에 추가하지 않고 별도 프롭 에셋으로 생성·배치 여부를 나중에 결정

## 2. 유닛 슬롯 타일 (크로마키, 1장 → 12슬롯 재사용)

```
single square pixel art game asset: a cracked flat stone slab tile,
top-down view, weathered gray stone with a faint carved rune,
16-bit retro RPG style, 1px dark outline, muted earthy palette,
solid green background (#00FF00), centered, no characters, no text
```

- 배치 가능/합성 강조 틴트는 코드(SpriteRenderer 색)로 — 색 변형 생성 금지

## 3. 중앙 = 소환사 + 소환진 데칼 (2026-07-11 개정 — 소환석 폐기)

중앙에는 소환사(주인공)가 직접 서 있고, 발밑에 소환진 데칼을 깐다. 소환사 HP = 코어 HP.
피격 단계 이미지는 불필요 — 소환사 피격은 엔진의 흰색 플래시 연출로 (SPEC §3). 에셋 2장 절약.

### 3a. 소환진 바닥 데칼 (크로마키) — `decal_summon_circle.png`

```
single pixel art game asset: a glowing purple magic summoning circle
etched into cracked dry ground, perfect circle seen directly from above,
runic symbols around the rim, faint purple glow,
16-bit retro RPG style, 1px dark outline, muted earthy palette,
solid green background (#00FF00), centered, no characters, no text
```

- 소환사 스프라이트가 위에 얹히므로 중심부는 문양을 비우는 게 좋음 — 복잡하게 나오면 `simple open center` 추가

### 3b. 소환사 전신 (크로마키) — `summoner_field.png` (캐릭터 앵커 사용)

환경 앵커가 아니라 **캐릭터 앵커**(SPEC §5.1 레이어 1) 기반. 필드 중앙 상시 노출 + 타이틀/로딩 겸용.

```
cute chibi pixel art game character: a young human summoner in a hooded
robe, holding a wooden staff with a glowing purple crystal,
full body, front view, standing idle pose,
16-bit retro RPG style, big head small body, 1px dark outline,
muted earthy palette, solid green background (#00FF00), centered, no text
```

- 로브·크리스탈의 보라는 소환진 데칼의 보라와 같은 계열로 — 어긋나면 한쪽을 에디터에서 색 보정
- 초상화(`summoner_portrait.png`)는 전신이 확정된 뒤 같은 캐릭터 서술로 `close-up portrait, head and shoulders`만 바꿔 생성

## 4. 스폰 지점 (보류 — 무경로 맵에서는 배경에 포함하지 않음)

몬스터 스폰 위치는 웨이브 데이터와 런타임 로직이 결정한다. 현재 채택 맵에는 스폰 지점 그래픽을 그리지 않는다. 필요할 때만 별도 월드 이펙트로 생성한다.

```
single pixel art game asset: a dark spawning pit in cracked dry ground,
bubbling green slime ooze seeping from the hole, top-down view,
16-bit retro RPG style, 1px dark outline, muted earthy palette,
solid green background (#00FF00), centered, no text
```

## 5. 데코 소품 시트 (크로마키, 선택 — 여유 시 1장)

통바닥이 허전할 때 얹는 낱개 소품. 한 장 그리드로 뽑아 잘라 쓴다 (SPEC §5.1 그리드 트릭).

```
pixel art sprite sheet of desolate forest props for a game, arranged
in a grid with clear spacing: a dead bare tree, a broken stump,
a mossy boulder, a pile of small rocks, a withered bush, a fallen log,
top-down friendly angle, 16-bit retro RPG style, 1px dark outline,
muted earthy palette, solid green background (#00FF00), no text
```

## 6. 실패 대응 치트시트

| 증상 | 추가/수정 키워드 |
|---|---|
| 픽셀이 아니라 일반 일러스트로 나옴 | `strict pixel art, visible pixel grid, low resolution sprite` 강조 |
| 화사한 초록 초원으로 나옴 | `withered, dry, autumn, desaturated` 강조 + `lush, vibrant green` 금지 서술 |
| 캐릭터/몬스터 혼입 | `no characters` 를 문장 맨 앞에도 반복 |
| 글자·워터마크 혼입 | `no text, no letters, no watermark` |
| 맵에 길·중앙 랜드마크·장식물이 생김 | `no paths, no focal point, no decorative props` 를 프롬프트 앞·뒤에 반복 |
| 시점이 측면으로 눕음 | `directly overhead, bird's eye view` 로 교체 |

## 7. 생성 체크리스트 (장당)

1. 프롬프트 복붙 → 2~3장 생성 → 베스트 선택
2. 원본 `ArtSource/` 저장 (크로마키 포함 그대로)
3. 크로마 제거 → 최근접 다운스케일 → `Assets/Art/` (같은 파일명)
4. [asset-ledger.csv](../asset-ledger.csv) 행 추가 (파일명/도구/프롬프트/라이선스)
5. v1이 실패했으면 [prompt-cases/](../prompt-cases/README.md)에 스크린샷과 함께 기록
