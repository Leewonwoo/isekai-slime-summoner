# UI 에셋 프롬프트 팩 — 고블린 숲 v3 (무경로 맵 톤 그라운딩)

> SPEC §5.1 3단 아키텍처의 **UI 프레임·아이콘 카테고리 실전 전개본**. ChatGPT 이미지 생성에 복붙해서 사용.
> 색 기준: 채택된 필드 배경 `ArtSource/core/ingame_stage_3.png` — 연속된 이끼 숲 바닥, 짙은 녹색 수관, 갈색 나무와 뿌리, 낮은 채도의 자연 팔레트. UI 토큰(ui-guidelines §4)과 동일 계열.
> 생성 후: 원본 `ArtSource/ui-frames/`·`ArtSource/ui-icons/` → 가공본 `Assets/Art/` → [asset-ledger.csv](../asset-ledger.csv) 기록.

## 0. 공통 규격

- **UI 앵커** (모든 프롬프트 끝에 포함돼 있음):
  ```
  16-bit retro RPG pixel art game UI asset, clean chunky pixels,
  muted earthy palette (weathered stone gray, warm tan wood,
  dark reddish-brown outlines), solid green background (#00FF00), no text
  ```
- 전부 크로마키. 후처리·임포트는 배경과 동일 (최근접 다운스케일, Point, Compression None — SPEC §5.1)
- **9-slice 적용 절차는 ui-guidelines §4 "픽셀 프레임" 절** — 적용 시 슬라이스 px 값을 그 문서에 기록
- 레드닷·합성 뱃지·게이지 fill은 **생성하지 않음** (USS 플랫 처리 유지 — 색 의미가 토큰에 걸려 있음)

## 1. 9-slice 프레임 5종 (`ArtSource/ui-frames/`)

### F1. 돌 패널 프레임 — `frame_stone_panel.png` (하단 패널/HUD 외곽)

```
pixel art UI panel frame for a retro RPG game: rectangular border frame
of weathered gray stone bricks, dry and cracked like ruins in a desolate
autumn forest, plain dark flat center fill, designed for 9-slice scaling
(uniform border thickness on all four sides, identical corners),
16-bit retro RPG pixel art game UI asset, clean chunky pixels,
muted earthy palette, solid green background (#00FF00), no text
```

### F2. 나무 버튼 — `frame_wood_button.png` (일반 버튼·리스트 행·활성 탭)

```
pixel art UI button for a retro RPG game: rectangular button of warm tan
wooden planks with subtle uniform wood grain, dark reddish-brown border,
slightly beveled top edge and darker bottom edge (chunky pressed look),
designed for 9-slice scaling (uniform border, identical corners),
16-bit retro RPG pixel art game UI asset, clean chunky pixels,
muted earthy palette, solid green background (#00FF00), no text
```

- 눌림 상태는 별도 생성 없이 USS `:active`(하단 엣지 축소+틴트)로

### F3. 골드 버튼 — `frame_gold_button.png` (소환 버튼 등 주요 행동 전용)

```
pixel art UI button for a retro RPG game: rectangular button of faded
gilded metal, warm antique gold face with darker bronze border and
slightly beveled edges, worn but noble, designed for 9-slice scaling
(uniform border, identical corners),
16-bit retro RPG pixel art game UI asset, clean chunky pixels,
muted earthy palette with warm gold highlights,
solid green background (#00FF00), no text
```

### F4. 돌 홈 슬롯 — `frame_slot_recess.png` (벤치 12칸·장비 슬롯)

```
pixel art UI slot for a retro RPG game: square recessed stone socket,
sunken dark inner area with a thin weathered gray stone rim,
looks carved into rock (opposite of a raised button),
designed for 9-slice scaling (uniform rim, identical corners),
16-bit retro RPG pixel art game UI asset, clean chunky pixels,
muted earthy palette, solid green background (#00FF00), no text
```

### F5. 원형 링 프레임 — `frame_ring.png` (스킬 플로팅 버튼·소환사 초상화 테)

원형은 9-slice 불가 — **고정 사이즈 통짜**로 쓴다 (스킬 버튼 128px, 초상화 104px 두 용도라 정사각 1장 생성 후 스케일).

```
pixel art UI circular frame for a retro RPG game: a round ring frame of
weathered gray stone with faded gold rivets, empty transparent center,
16-bit retro RPG pixel art game UI asset, clean chunky pixels,
muted earthy palette, solid green background (#00FF00), centered, no text
```

## 2. 아이콘 시트 4장 (`ArtSource/ui-icons/`, 그리드 시트 트릭 — SPEC §5.1)

한 장에 그리드로 생성 → 슬라이스. 시트 단위로 스타일 일관성 확보. 아이콘 앵커:

```
pixel art game icon set for a retro RPG, {N} icons arranged in a single
row with equal cells and clear spacing, bold readable silhouettes that
stay clear at small size, 1px dark outline, muted earthy palette with
warm highlights, solid green background (#00FF00), no text, no labels
```

### I1. 재화·공통 4종 — `icons_common_sheet.png` → `icon_gold / icon_gem / icon_gear / icon_star`

```
{N}=4, the icons are: (1) a gold coin with an embossed emblem,
(2) a cut blue gemstone, (3) a gray iron gear, (4) a five-pointed gold star
```

### I2. 강화 스탯 6종 — `icons_stat_sheet.png` → `icon_atk / icon_aspd / icon_crit / icon_hp / icon_range / icon_income`

SPEC §2.4 인게임 강화 4종(공격력/공속/소환사 HP 회복/치명타) + 예비 2종(사거리/골드 획득).
실제 생성본은 정사각 캔버스의 **3열×2행**으로 배치한다: 상단 `atk / aspd / crit`, 하단 `hp / range / income`.

```
{N}=6, the icons are: (1) an upward sword, (2) two curved speed arrows,
(3) a crosshair with a spark, (4) a red heart with a small plus,
(5) a bow with a long arrow, (6) a small pouch of coins
```

### I3. 속성 4종 — `icons_attr_sheet.png` → `icon_attr_none / fire / ice / nature` (SPEC §2.8, 방향 예고 배지 안 표시)

```
{N}=4, the icons are: (1) a plain gray circle emblem (neutral),
(2) an orange-red flame, (3) a light blue snowflake, (4) a green leaf
```

- 배지 배경(빨강/노랑/회색) 위에서도 읽혀야 함 — 실루엣이 약하면 `thicker outline, simpler shape` 추가

### I4. 스킬·장비 6종 — `icons_skill_gear_sheet.png` → `icon_meteor / icon_summon / icon_weapon / icon_armor / icon_ring / icon_scroll`

```
{N}=6, the icons are: (1) a falling meteor with a fire trail,
(2) a glowing purple summoning rune circle, (3) a crossed sword and axe,
(4) a leather chest armor, (5) a simple gold ring with a purple gem,
(6) a rolled parchment scroll
```

## 3. 실패 대응 치트시트 (UI 특화)

| 증상 | 추가/수정 키워드 |
|---|---|
| 그리드 셀 크기 제각각 | `all icons the same size, evenly spaced in one row` |
| 프레임 네 변 두께 불균일 (9-slice 불가) | `uniform border thickness on all four sides, symmetrical corners` 강조 |
| 프레임 중앙에 장식 혼입 | `plain empty center, decoration only on the border` |
| 아이콘이 사실화풍으로 나옴 | `flat game icon, bold silhouette, low resolution sprite` |
| 시트 안 스타일 흔들림 | `all icons in the exact same style and palette` |
| 금색이 노란 플라스틱처럼 나옴 | `antique faded gold, bronze shadows` |

## 4. 생성 체크리스트 (장당)

1. 프롬프트 복붙 → 2~3장 생성 → 베스트 선택
2. 원본 `ArtSource/ui-frames/` 또는 `ArtSource/ui-icons/` 저장
3. 크로마 제거 → 최근접 다운스케일 → 시트는 슬라이스 → `Assets/Art/UIIcons/` (프레임은 `Assets/Art/UIFrames/`)
4. [asset-ledger.csv](../asset-ledger.csv) 행 추가 (슬라이스된 낱개 파일명 기준)
5. 프레임은 ui-guidelines §4에 슬라이스 px 값 기록 후 USS `background-image` + `-unity-slice-*` 적용
6. 실패→개선 시 [prompt-cases/](../prompt-cases/README.md) 기록
