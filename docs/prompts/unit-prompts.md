# 고블린 유닛 프롬프트 팩 — 척박한 숲 v2 (레거시)

> 2026-07-14 세계관 피벗으로 현재 아군 유닛은 슬라임으로 전환 중이다. 현재 사용 프롬프트는 [slime-prompts.md](slime-prompts.md)이며, 이 문서는 기존 고블린안의 기록 보존용이다.

> SPEC §5.1 3단 아키텍처의 **유닛 카테고리 실전 전개본**. 유닛당 **시트 1장 생성 → 3컷 슬라이스** (총 생성 8장 → 24컷).
> 뷰↔레인 대응: 정면=남 레인, 후면=북 레인, 측면(우향)=동 레인, 서 레인은 측면 플립 (SPEC §5.1).
> 성급(★1~★3) 스프라이트 재생성 금지 — 틴트 + 별 뱃지, 진화는 이름·툴팁·이펙트로 (SPEC §2.3).
> 생성 후: 원본 `ArtSource/units/` → 슬라이스 가공본 `Assets/Art/Units/` → [asset-ledger.csv](../asset-ledger.csv) 기록.

## 0. 공통 규격

- **고블린 베이스 서술 (전 유닛 동일 문구 — 종족 일관성 앵커)**: 모든 프롬프트에 아래 문구를 그대로 쓴다. 단어를 바꾸면 유닛 간 종족감이 흔들린다.
  ```
  a short stocky goblin with dull green skin, long pointy ears
  and big yellow eyes
  ```
- **시트 템플릿 (유닛당 이것 하나, {unit_concept}만 치환)**:
  ```
  pixel art character sheet of the same goblin game character,
  3 full-body views in a horizontal row: front view, back view,
  side view facing right, identical character and outfit in all
  three views, standing idle pose with weapon visible, evenly spaced —
  the character is a short stocky goblin with dull green skin,
  long pointy ears and big yellow eyes, {unit_concept},
  cute chibi proportions (about 2 heads tall), 16-bit retro RPG style,
  1px dark outline, muted earthy palette with attribute-colored accents,
  solid green background (#00FF00), no text
  ```
- 속성은 **무기·소품 색으로만** 표현: 무=쇠·가죽 무채색 / 화염=주황·빨강 / 빙결=얼음 하늘색 / 자연=독 연두. 몸통 팔레트는 전 유닛 공통 어스 톤 유지 (배경 `ingame_stage_3.png`와 동일 계열)
- 시즈 모드(SPEC §2.1)라 이동 포즈 불필요 — **정지 대기 포즈 고정**. 공격은 DOTween 제자리 연출
- 후처리·임포트는 공통 규격 (최근접 다운스케일, Point, Compression None — SPEC §5.1)

## 1. 개체 변수 8종 — `{unit_concept}`

| # | 유닛 (파일명) | 속성 | {unit_concept} |
|---|---|---|---|
| 1 | 전사 `unit_goblin_warrior` | 무 | `a melee warrior holding a chipped iron sword and a small round wooden shield, wearing scrap leather armor` |
| 2 | 궁수 `unit_goblin_archer` | 무 | `an archer holding a short wooden bow, a quiver of arrows on his back, wearing a ragged hood` |
| 3 | 화염술사 `unit_goblin_fire_shaman` | 화염 | `a fire shaman holding a torch staff with a burning orange flame, wearing a red-trimmed tattered robe` |
| 4 | 냉기술사 `unit_goblin_frost_mage` | 빙결 | `a frost mage holding a gnarled staff topped with a glowing light-blue ice crystal, wearing a pale blue-trimmed robe` |
| 5 | 독 주술사 `unit_goblin_poison_shaman` | 자연 | `a poison shaman wearing a tribal bone mask, holding a crooked staff with a bubbling green venom flask hanging from it` |
| 6 | 북잡이 `unit_goblin_drummer` | 무 | `a war drummer carrying a big wooden war drum on his belly, holding two drumsticks, wearing a feathered cap` |
| 7 | 폭탄병 `unit_goblin_bomber` | 화염 | `a bomber holding a round black bomb with a lit fuse, carrying a backpack full of bombs, wearing goggles` |
| 8 | 저격수 `unit_goblin_sniper` | 빙결 | `a sniper aiming a long heavy crossbow loaded with an ice-blue crystal bolt, wearing a fur-lined hood and an eyepatch` |

- 8종은 **실루엣만으로 구분**돼야 함(전장 가독성): 방패/활/횃불지팡이/수정지팡이/가면/북/폭탄가방/장궁쇠뇌. 실루엣이 비슷하게 나오면 소품을 키우는 방향으로 수정
- 냉기술사 vs 저격수(둘 다 빙결): 지팡이 vs 쇠뇌로 무기 계열 자체를 분리

## 2. 조립 예시 (전사 — 이대로 복붙)

```
pixel art character sheet of the same goblin game character,
3 full-body views in a horizontal row: front view, back view,
side view facing right, identical character and outfit in all
three views, standing idle pose with weapon visible, evenly spaced —
the character is a short stocky goblin with dull green skin,
long pointy ears and big yellow eyes, a melee warrior holding
a chipped iron sword and a small round wooden shield,
wearing scrap leather armor,
cute chibi proportions (about 2 heads tall), 16-bit retro RPG style,
1px dark outline, muted earthy palette with attribute-colored accents,
solid green background (#00FF00), no text
```

## 3. 실패 대응 치트시트 (캐릭터 시트 특화)

| 증상 | 추가/수정 키워드 |
|---|---|
| 3뷰가 서로 다른 캐릭터로 나옴 | `identical character and outfit in all three views` 강조, 의상 서술을 더 구체화 |
| 후면 뷰에 얼굴이 보임 | `back view shows the back of the head, no face visible` |
| 후면 뷰에서 무기 실종 | `weapon visible in all three views` |
| 뷰가 2개나 4개로 나옴 | `exactly 3 views` + 실패 시 재생성이 빠름 |
| 등신이 큼 (리얼 비율) | `chibi, about 2 heads tall, big head small body` 강조 |
| 몸 색이 쨍한 초록 | `dull desaturated green skin` |
| 유닛 간 톤 불일치 | 베이스 서술 문구를 한 글자도 바꾸지 말 것 (§0) |

## 4. 생성 순서·체크리스트

**순서**: ① 전사 2~3장으로 톤·비율 확정 (1주차 테스트 생성 — SPEC §6) → ② 기본 해금 4종(전사/궁수/화염술사/냉기술사) 완성 → ③ 메타 해금 4종. 전사가 확정되기 전에 나머지를 뽑지 말 것 — 베이스 서술이 흔들리면 8종 전부 재생성.

장당:
1. 시트 생성 → 베스트 선택 → 원본 `ArtSource/units/unit_goblin_*.png`
2. 크로마 제거 → 최근접 다운스케일 → 3컷 슬라이스 → `Assets/Art/Units/unit_goblin_*_f.png / _b.png / _s.png`
3. [asset-ledger.csv](../asset-ledger.csv)에 시트 원본 + 슬라이스 3컷 기록
4. 실패→개선 시 [prompt-cases/](../prompt-cases/README.md) 기록 (3뷰 일관성 실패가 나올 확률이 높음 — 사례 2호 후보)
