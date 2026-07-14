# ArtSource — AI 생성 이미지 원본 보관소

AI로 생성한 **원본 이미지**(크로마키 그린 배경, 가공 전)를 여기에 넣는다.
Unity가 임포트하지 않도록 `Assets/` 밖에 둔다. 원본 자체가 AI 활용 증거이므로 저장소에 커밋한다.

## 워크플로

1. 생성한 원본을 카테고리 폴더에 저장 (아래 파일명 규칙)
2. [docs/asset-ledger.csv](../docs/asset-ledger.csv)에 한 줄 기록 (파일명/도구/프롬프트 전문/라이선스)
3. 배경 제거·크롭 가공 후 **같은 파일명**으로 `Assets/Art/해당폴더/`에 저장
4. 실패→개선을 겪었으면 [docs/prompt-cases/](../docs/prompt-cases/README.md)에 사례 기록 (v1.png, v2.png)

## 파일명 규칙 (snake_case, 에셋 대장과 반드시 일치)

| 카테고리 | 폴더 | 파일명 예 |
|---|---|---|
| 유닛 | `units/` | `unit_frost_archer.png`, `unit_flame_golem.png` |
| 적 | `enemies/` | `enemy_slime.png`, `boss_dragon.png` (보스도 여기, `boss_` 접두사) |
| 중앙(소환사·소환진) | `core/` | `summoner_field.png`(전신), `summoner_portrait.png`, `decal_summon_circle.png` — 소환석 피격 단계 이미지는 폐기 (SPEC §2.1 개정) |
| 타일/바닥 | `tiles/` | `tile_lane.png`, `tile_slot.png` |
| UI 아이콘 | `ui-icons/` | `icons_stat_sheet.png`(시트 원본), `icon_gold.png`, `icon_atk.png` |
| UI 프레임 (9-slice) | `ui-frames/` | `frame_stone_panel.png`, `frame_wood_button.png`, `frame_gold_button.png`, `frame_slot_recess.png`, `frame_ring.png` |
| 일러스트 | `illustrations/` | `illust_title.png`, `illust_loading.png` |

- 버전이 생기면 `_v2` 접미사 (`unit_frost_archer_v2.png`) — 최종 채택본만 Assets/Art로
- 등급별 색 변형은 이미지로 만들지 않는다 (Unity 틴트 처리, SPEC §2.3)

## 대응하는 게임용 폴더 (가공 완료본)

```
Assets/Art/
  Units/  Enemies/  Core/  Tiles/  UIIcons/  UIFrames/  Illustrations/
```
