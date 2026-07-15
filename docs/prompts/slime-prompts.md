# 슬라임 유닛 스프라이트 프롬프트 팩 — 16-bit 숲 생존 디펜스

> 현재 아군 유닛용 활성 프롬프트 팩. 기본형 8종을 먼저 생성하고, ★1~★3은 Unity의 크기·틴트·장식·이펙트로 확장한다.
> 생성 후 원본은 `ArtSource/units/`, 가공본은 `Assets/Art/Units/`, [asset-ledger.csv](../asset-ledger.csv)에 기록한다.

## 0. 공통 규격

- strict 16-bit retro pixel art, clean chunky pixel clusters, 1px dark outline
- 귀엽고 둥근 치비 슬라임, 작은 모바일 화면에서도 역할이 읽히는 강한 실루엣
- 정면 단일 스프라이트, idle pose, 단일 개체, 여백 포함
- 기본 몸체에는 **팔·주먹·다리·얼굴을 그리지 않음**. 이동과 기본 공격은 각각 3×3 9프레임 시트로 처리
- 기본·★1은 **눈·입·코·얼굴 무표정 요소를 전부 금지**. ★2부터 눈·입을 추가하고 ★3에서 개성 있는 표정을 허용
- 배경은 완전히 평평한 `#00FF00` 크로마키. 그림자·바닥·반사·텍스트·워터마크 금지
- 후처리: 크로마 제거 → 알파 바운딩 크롭 → 128×128px 최근접 다운스케일·패딩
- Unity 임포트: Filter **Point**, Compression **None**, PPU **200**, Mipmap **Off**

## 1. 기본형 로스터

| 파일명 | 유닛 | 속성 | 역할 |
|---|---|---|---|
| `unit_punch_slime.png` | 주먹 슬라임 | 무 | 근접 단일 공격 |
| `unit_watergun_slime.png` | 물총 슬라임 | 무 | 원거리 단일 공격 |
| `unit_ember_slime.png` | 불씨 슬라임 | 화염 | 범위 공격 |
| `unit_frost_slime.png` | 서리 슬라임 | 빙결 | 감속·제어 |
| `unit_sprout_slime.png` | 새싹 슬라임 | 자연 | 지속 피해 |
| `unit_resonance_slime.png` | 공명 슬라임 | 무 | 아군 공속 버프 |
| `unit_burst_slime.png` | 팽창 슬라임 | 화염 | 단거리 폭발 |
| `unit_crystal_slime.png` | 빙정 슬라임 | 빙결 | 장거리 관통 |

## 2. 스타일 앵커 — 주먹 슬라임

```text
Use case: stylized-concept
Asset type: Unity 2D mobile tower-defense unit sprite, base-rank ally slime
Primary request: Create a single base-rank neutral Punch Slime unit for a cute 16-bit pixel-art tower-defense game. It is a friendly summoned slime that attacks nearby goblin enemies with a simple punch.
Scene/backdrop: perfectly flat solid chroma-key green background #00FF00 for later background removal. No ground plane, no shadow, no lighting gradient.
Subject: one compact rounded jelly slime body only, slightly asymmetrical but highly readable, muted pale gray-blue translucent body with a thick dark pixel outline, no eyes, no mouth, no face, no arms, no hands, no legs, no weapon, no armor, no clothing, no extra objects. The silhouette must read clearly at small mobile-game size.
Style/medium: strict 16-bit retro RPG pixel art, cute chibi game sprite, clean chunky pixel clusters, visible pixel-grid feeling, restrained earthy palette, 1px dark outline, polished production game asset.
Composition/framing: front view, centered, full body, idle standing pose with one fist raised, generous empty padding on all sides, single subject only, square image.
Lighting/mood: friendly, brave, slightly comedic, flat sprite lighting with no cast shadow.
Color palette: neutral gray-blue body, dark brown-gray outline, tiny warm cream highlights, no attribute color glow.
Constraints: base rank only, completely featureless face with no eyes, no mouth, no nose, and no facial markings; no star icon, no text, no UI, no watermark, no motion blur, no multiple views, no sprite sheet, no cropped edges. Do not use the chroma-key green anywhere in the slime.
Avoid: realistic 3D rendering, glossy toy render, anime illustration, human features, goblin parts, weapons, hats, armor, fire, ice, leaves, props, background texture, floor, cast shadow, reflection, letters, logos, watermark.
```

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
- 이동·공격 시트 모두 셀 간격·프레임 순서를 고정하고, 몸체의 기준점이 흔들리지 않게 Unity Sprite Editor에서 3×3 고정 그리드로 슬라이스한다.

### 이동 시트 프롬프트

```text
Create a strict 3 by 3 sprite sheet containing exactly 9 animation frames of the same featureless slime body performing a gentle bouncy poing-poing movement loop. The slime should squash downward, stretch slightly upward, lift a tiny amount, and settle back down across the sequence. Exactly 9 equal square cells, one slime in each cell, no overlap, no gutters, no frame borders, no labels. Preserve the same pale gray-blue body, no face, no eyes, no mouth, no arms, no fists, no legs, flat #00FF00 chroma-key background, strict 16-bit pixel art.
```

### 공격 시트 프롬프트

```text
Create a strict 3 by 3 sprite sheet containing exactly 9 frames of the same featureless Punch Slime attack, following a rough storyboard reference. Use only one punch on the slime's right side (viewer right). Frame 1 is the resting body, frame 2 is a slight squash preparation, frame 3 is a tiny attached fist nub, frame 4 shows a short thick arm and fist emerging diagonally upward-right, frame 5 is the strongest fully extended punch, frame 6 begins the retraction, frame 7 leaves a tiny right-side bump, frame 8 settles, and frame 9 returns to the featureless body. The arm must remain attached and the fist must be compact, blunt, rounded, and slightly irregular like a simple slime fist. Do not create a left arm, a second fist, bilateral arms, a long smooth one-sided sausage, thin noodle arms, floating fists, gloves, or projectiles. Exactly 9 equal square cells, no overlap, no gutters, no borders, no labels. Same pale gray-blue body, no face, no permanent limbs, flat #00FF00 chroma-key background, strict 16-bit pixel art.
```
