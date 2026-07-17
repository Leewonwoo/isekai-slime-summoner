# 슬라임 유닛 스프라이트 프롬프트 팩 — 16-bit 숲 생존 디펜스

> 현재 아군 유닛용 활성 프롬프트 팩. 기본형 8종을 먼저 생성하고, ★1~★3은 Unity의 크기·틴트·장식·이펙트로 확장한다.
> 생성 후 원본은 `ArtSource/units/`, 가공본은 `Assets/Art/Units/`, [asset-ledger.csv](../asset-ledger.csv)에 기록한다.

## 0. 공통 규격

> 모든 이동·공격 시트는 [캐릭터 애니메이션 공통 마스터 프롬프트](animation-master-prompt.md)의 `SLIME` 프로필을 사용한다. 몸체 밑면 중앙을 루트로 고정하며, 임시 팔·주먹·분사·역할 장식은 정렬 및 크기 계산에서 제외한다.

- strict 16-bit retro pixel art, clean chunky pixel clusters, 1px dark outline
- 귀엽고 둥근 치비 슬라임, 작은 모바일 화면에서도 역할이 읽히는 강한 실루엣
- 정면 단일 스프라이트, idle pose, 단일 개체, 여백 포함
- 기본 몸체에는 **팔·주먹·다리·얼굴을 그리지 않음**. 이동과 기본 공격은 각각 3×3 9프레임 시트로 처리
- 기본·★1은 **눈·입·코·얼굴 무표정 요소를 전부 금지**. ★2부터 눈·입을 추가하고 ★3에서 개성 있는 표정을 허용
- 배경은 완전히 평평한 `#00FF00` 크로마키. 그림자·바닥·반사·텍스트·워터마크 금지
- 단일 idle 스프라이트 후처리: 크로마 제거 → 알파 바운딩 크롭 → 128×128px 최근접 다운스케일·패딩
- 애니메이션 시트 후처리: 시트 전체 384×384 유지 → 프레임별 크롭 없이 128×128 고정 셀 슬라이스
- Unity 임포트: Filter **Point**, Compression **None**, PPU **200**, Mipmap **Off**

## 1. 기본형 로스터

| 파일명 | 유닛 | 속성 | 역할 |
|---|---|---|---|
| `unit_punch_slime.png` | 주먹 슬라임 | 무 | 근접 단일 공격 |
| `unit_watergun_slime.png` | 물총 슬라임 | 무 | 원거리 단일 공격 |
| `unit_flame_slime.png` | 불꽃 슬라임 | 화염 | 범위 공격 |
| `unit_ice_slime.png` | 얼음 슬라임 | 빙결 | 감속·제어 |
| `unit_green_slime.png` | 초록 슬라임 | 자연 | 지속 피해 |
| `unit_buff_slime.png` | 버프 슬라임 | 무 | 아군 공속 버프 |
| `unit_explosion_slime.png` | 폭발 슬라임 | 화염 | 단거리 폭발 |
| `unit_freeze_slime.png` | 빙결 슬라임 | 빙결 | 장거리 관통 |

## 2. 스타일 앵커 — 주먹 슬라임

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense unit sprite, base-rank ally slime
Primary request: Create a single base-rank neutral Punch Slime unit for a cute 16-bit pixel-art tower-defense game. It is a friendly summoned slime that attacks nearby goblin enemies with a simple punch.
Scene/backdrop: perfectly flat solid chroma-key green background #00FF00 for later background removal. No ground plane, no shadow, no lighting gradient.
Subject: one compact rounded jelly slime body only, slightly asymmetrical but highly readable, muted pale gray-blue translucent body with a thick dark pixel outline, no eyes, no mouth, no face, no arms, no hands, no legs, no weapon, no armor, no clothing, no extra objects. The silhouette must read clearly at small mobile-game size.
Style/medium: strict 16-bit retro RPG pixel art, cute chibi game sprite, clean chunky pixel clusters, visible pixel-grid feeling, restrained earthy palette, 1px dark outline, polished production game asset.
Composition/framing: front view, centered, full body, idle pose, generous empty padding on all sides, single subject only, square image.
Lighting/mood: friendly, brave, slightly comedic, flat sprite lighting with no cast shadow.
Color palette: neutral gray-blue body, dark brown-gray outline, tiny warm cream highlights, no attribute color glow.
Constraints: base rank only, completely featureless face with no eyes, no mouth, no nose, and no facial markings; no star icon, no text, no UI, no watermark, no motion blur, no multiple views, no sprite sheet, no cropped edges. Do not use the chroma-key green anywhere in the slime.
Avoid: realistic 3D rendering, glossy toy render, anime illustration, human features, goblin parts, weapons, hats, armor, fire, ice, leaves, props, background texture, floor, cast shadow, reflection, letters, logos, watermark.
```

## 2.1 스타일 변형 — 불꽃 슬라임

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense unit sprite, base-rank ally Flame Slime
Primary request: Create a single base-rank Flame Slime, the fire-attribute ally slime for a cute 16-bit pixel-art tower-defense game. It is a friendly summoned slime whose fire identity must be readable from its silhouette alone.
Input image: use the Punch Slime style anchor as a production-spec reference only; create a new distinct fire-attribute sprite.
Scene/backdrop: perfectly flat solid chroma-key green background #00FF00 for later background removal. No ground plane, shadow, lighting gradient, or props.
Subject: one compact squat rounded slime body with an integrated small licking flame crest rising from the top center. Warm orange-red molten jelly body, deep red-brown pixel outline, bright yellow-orange inner flame/core, a few cream-yellow pixel highlights. No eyes, no mouth, no nose, no face, no arms, no hands, no legs, no weapon, no armor, no clothing, no separate hat, crown, aura, smoke, sparks, or fireball.
Style/medium: strict 16-bit retro RPG pixel art, same chunky low-detail silhouette language as the Punch Slime anchor, crisp pixel clusters, polished mobile game asset.
Composition/framing: front view, centered, full body, idle pose, generous empty padding, single subject, square image, no cropped edges.
Constraints: base rank only; no facial features; the flame must be integrated into the slime silhouette and only slightly taller than the body; no text, no UI, no watermark, no sprite sheet, no multiple views.
Avoid: realistic 3D rendering, glossy toy render, anime illustration, separate fire effect, smoke, sparks, fireball, weapon, prop, background texture, floor, cast shadow, reflection, letters, logos, watermark.
```

## 2.2 역할별 스프라이트 변형

각 기본형은 Punch Slime의 몸체 비율과 제작 규격을 유지하고, 얼굴·팔다리 대신 색상과 몸체에 통합된 작은 실루엣으로 역할을 구분한다.

| 파일명 | 역할 실루엣 지시 |
|---|---|
| `unit_watergun_slime.png` | 시안색 몸체, 상단 물방울 crest, 오른쪽 짧은 물 분사형 돌기 |
| `unit_ice_slime.png` | 옅은 얼음색 몸체, 상단의 작고 각진 서리 결정 2~3개 |
| `unit_green_slime.png` | 밝은 초록 몸체와 이끼색 하단 음영. 잎·꽃·가지 없이 색상으로만 표현 |
| `unit_buff_slime.png` | 보라·청록 몸체, 몸체에 통합된 작은 금색 상승 chevron |
| `unit_explosion_slime.png` | 빨강·주황 몸체, 짧은 퓨즈 돌기와 따뜻한 내부 폭발 코어 |
| `unit_freeze_slime.png` | 진한 파랑·보라 몸체, 오른쪽 상단의 큰 얼음 파편 crest |

공통 금지 요소: 눈·입·코·얼굴, 팔·손·다리, 분리된 장식품, 부유 이펙트, 텍스트, UI, 배경 오브젝트.

## 3. 등급 확장 규칙

- ★1: 얼굴 없음 유지 + 기본 실루엣 유지 + 크기 108% + 작은 윤곽 하이라이트
- ★2: 크기 116% + 눈·입 추가 + 역할을 보여주는 작은 형태 변화
- ★3: 크기 125% + 눈·입의 개성 있는 표정 + 왕관·오라·속성 이펙트 중 하나만 추가
- 기본형과 등급형을 각각 새로 생성하지 않는다. 머지 시스템은 동일 스프라이트의 등급 파라미터로 먼저 구현한다.

## 4. 이동·공격 연출 규칙 — 주먹 슬라임

- 이동에는 `unit_punch_slime_move_sheet.png` 3×3 9프레임 시트를 사용한다. 프레임은 squash → stretch → settle 순서의 반복 루프다.
- 이동 시트는 이동 방향을 표현하지 않는 포잉포잉 루프 전용이며, 유닛의 실제 위치 이동은 기존 시즈 모드 규칙을 따른다.
- 기본 몸체는 고정하고, `unit_punch_slime_attack_sheet.png`를 3×3 9프레임으로 재생한다.
- 공격 시 슬라임 오른쪽에서만 짧은 팔이 생기고, 끝의 둥근 주먹이 대각선으로 전진 → 최대 돌출 유지 → 원위치 복귀한다.
- 왼쪽 팔·두 번째 주먹·분리된 주먹은 금지한다. 팔은 몸체에 붙은 짧고 두꺼운 형태이며, 주먹은 뭉툭하고 약간 불규칙한 주먹 실루엣으로 읽혀야 한다.
- 이동·공격 시트 모두 마스터 프롬프트의 `SLIME` 루트 규칙을 적용하고, Unity Sprite Editor에서 128×128 고정 셀·동일 Center 피벗으로 슬라이스한다.

### 이동 시트 프롬프트

```text
Create a strict 3 by 3 sprite sheet containing exactly 9 animation frames of the same featureless slime body performing a gentle bouncy poing-poing movement loop. The slime should squash downward, stretch slightly upward, lift a tiny amount, and settle back down across the sequence. Exactly 9 equal square cells, one slime in each cell, no overlap, no gutters, no frame borders, no labels. Preserve the same pale gray-blue body, no face, no eyes, no mouth, no arms, no fists, no legs, flat #00FF00 chroma-key background, strict 16-bit pixel art.
```

### 공격 시트 프롬프트

```text
Create a strict 3 by 3 sprite sheet containing exactly 9 frames of the same featureless Punch Slime attack, following a rough storyboard reference. Use only one punch on the slime's right side (viewer right). Frame 1 is the resting body, frame 2 is a slight squash preparation, frame 3 is a tiny attached fist nub, frame 4 shows a short thick arm and fist emerging diagonally upward-right, frame 5 is the strongest fully extended punch, frame 6 begins the retraction, frame 7 leaves a tiny right-side bump, frame 8 settles, and frame 9 returns to the featureless body. The arm must remain attached and the fist must be compact, blunt, rounded, and slightly irregular like a simple slime fist. Do not create a left arm, a second fist, bilateral arms, a long smooth one-sided sausage, thin noodle arms, floating fists, gloves, or projectiles. Exactly 9 equal square cells, no overlap, no gutters, no borders, no labels. Same pale gray-blue body, no face, no permanent limbs, flat #00FF00 chroma-key background, strict 16-bit pixel art.
```
