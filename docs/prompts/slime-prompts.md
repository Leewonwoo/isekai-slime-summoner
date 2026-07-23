# 슬라임 유닛 스프라이트 프롬프트 팩 — 16-bit 숲 생존 디펜스

> 현재 아군 유닛용 활성 프롬프트 팩. ★1 기본형 8종과 ★2 전용 진화형 8종을 사용한다. ★3은 전용 이미지 제작 전까지 ★2 이미지의 런타임 연출로 표현하되, 주먹·물총·초록 슬라임은 전용 ★3 진화형을 제작한다.
> 생성 후 원본은 `ArtSource/units/`, 가공본은 `Assets/Art/Units/`, [asset-ledger.csv](../asset-ledger.csv)에 기록한다.

## 0. 공통 규격

> 모든 이동·공격 시트는 [캐릭터 애니메이션 공통 마스터 프롬프트](animation-master-prompt.md)의 `SLIME` 프로필을 사용한다. 몸체 밑면 중앙을 루트로 고정하며, 임시 팔·주먹·분사·역할 장식은 정렬 및 크기 계산에서 제외한다.

- strict 16-bit retro pixel art, clean chunky pixel clusters, 1px dark outline
- 귀엽고 둥근 치비 슬라임, 작은 모바일 화면에서도 역할이 읽히는 강한 실루엣
- 정면 단일 스프라이트, idle pose, 단일 개체, 여백 포함
- ★1 기본 몸체에는 **팔·주먹·다리·얼굴을 그리지 않음**. 이동과 기본 공격은 각각 3×3 9프레임 시트로 처리
- ★1은 **눈·입·코·얼굴 무표정 요소를 전부 금지**. ★2부터 눈·입을 추가하고 ★3에서 개성 있는 표정을 허용
- 배경은 완전히 평평한 `#00FF00` 크로마키. 그림자·바닥·반사·텍스트·워터마크 금지
- 단일 idle 스프라이트 후처리: 크로마 제거 → 알파 바운딩 크롭 → 128×128px 최근접 다운스케일·패딩
- 애니메이션 시트 후처리: 시트 전체 384×384 유지 → 프레임별 크롭 없이 128×128 고정 셀 슬라이스
- Unity 임포트: Filter **Point**, Compression **None**, PPU **200**, Mipmap **Off**

## 1. 기본형 로스터

| 기본형 파일명 | ★2 파일명 | 유닛 | 속성 | 역할 |
|---|---|---|---|---|
| `unit_punch_slime.png` | `unit_punch_slime_star2.png` | 주먹 슬라임 | 무 | 근접 단일 공격 |
| `unit_watergun_slime.png` | `unit_watergun_slime_star2.png` | 물총 슬라임 | 무 | 원거리 단일 공격 |
| `unit_flame_slime.png` | `unit_flame_slime_star2.png` | 불꽃 슬라임 | 화염 | 범위 공격 |
| `unit_ice_slime.png` | `unit_ice_slime_star2.png` | 얼음 슬라임 | 빙결 | 감속·제어 |
| `unit_green_slime.png` | `unit_green_slime_star2.png` | 초록 슬라임 | 자연 | 지속 피해 |
| `unit_buff_slime.png` | `unit_buff_slime_star2.png` | 버프 슬라임 | 무 | 아군 공속 버프 |
| `unit_explosion_slime.png` | `unit_explosion_slime_star2.png` | 폭발 슬라임 | 화염 | 단거리 폭발 |
| `unit_freeze_slime.png` | `unit_freeze_slime_star2.png` | 빙결 슬라임 | 빙결 | 장거리 관통 |

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

- ★1: 얼굴 없음 유지 + 기본 실루엣 + 기준 크기 100%
- ★2: 전용 `_star2` 스프라이트 + 크기 120% + 공통의 단순한 눈·입 + 역할을 보여주는 작은 형태 변화. 주먹 슬라임은 예외적으로 양팔이 처음 생긴다.
- ★3: 전용 `_star3` 스프라이트 + 크기 140% + 왕관·오라·속성 이펙트 중 하나만 런타임으로 추가. 주먹 슬라임은 근육 전사형을 사용한다.
- 이미지 자체의 주 몸체 크기는 기본형과 동일하게 가공한다. 100%·120%·140% 크기 증가는 Unity 등급 모디파이어만 담당한다.

| 유닛 | ★2 진화 변화 |
|---|---|
| 주먹 | ★1의 둥근 젤리 몸체를 유지하면서 작은 눈·입과 좌우 양팔이 생김. 손은 단순한 둥근 주먹 형태 |
| 물총 | ★1의 상단 물방울 뿔과 몸체를 유지하면서 좌우 양쪽에 대칭형 분사구가 생김 |
| 불꽃 | 몸체에 붙은 2갈래 불꽃 crest + 밝아진 복부 코어 |
| 얼음 | 중앙 결정 상승 + 좌우 결정 각도 강화 + 소수의 연결 균열 |
| 초록 | ★1의 밝은 초록 젤리 몸체에 눈·입과 뒤쪽으로 휘어진 큰 잎 1장이 생김 |
| 버프 | 얼굴 아래 몸체에 통합된 금색 상승 chevron 2단 |
| 폭발 | 굵어진 짧은 퓨즈 + 밝은 끝점 + 복부 폭발 코어 링 |
| 빙결 | 오른쪽 상단의 연결된 3갈래 얼음 결정 + 강화된 균열 |

### 주먹 슬라임 성급별 진화

| 성급 | 외형 | 이동·공격 방향 |
|---|---|---|
| ★1 | 눈·입·팔다리가 없는 둥근 젤리 몸체 | 포잉포잉 이동, 공격 순간에만 임시 주먹이 뻗어 나옴 |
| ★2 | ★1 몸체 비율을 유지하며 작은 눈·입과 양팔이 생긴 격투가형 | 몸체로 튀어 이동하고, 상시 존재하는 양팔 중 한쪽으로 펀치 |
| ★3 | ★2의 얼굴과 양팔을 계승하고 근육질 상체와 짧고 굵은 두 다리가 생긴 근육 전사형 | 두 발로 걸어 이동하고, 몸통 회전을 이용한 강한 펀치 |

- ★3도 슬라임의 젤리 재질과 둥근 머리 실루엣을 유지한다. 사람 피부나 인간 얼굴처럼 보이지 않게 한다.
- 근육은 보디빌더식 세부 묘사보다 큰 어깨·두꺼운 팔·가슴 형태를 단순한 픽셀 덩어리로 표현한다.
- ★2 파일은 `unit_punch_slime_star2.png`, ★3 파일은 `unit_punch_slime_star3.png`를 사용한다.
- 기존 ★2 무팔 이미지는 2026-07-19에 작은 양팔이 붙은 격투가형으로 재생성·교체했다.

### 물총 슬라임 성급별 진화

| 성급 | 외형 | 이동·공격 방향 |
|---|---|---|
| ★1 | 얼굴 없는 물방울 젤리 몸체, 상단 물방울 뿔과 오른쪽 짧은 분사 돌기 | 한 방향으로 빠른 물 탄환 발사 |
| ★2 | ★1 몸체와 상단 물방울 뿔을 유지하고, ★1 분사구보다 크고 굵은 좌우 대칭 분사구가 생김 | 양쪽 대형 분사구가 번갈아 반동하는 빠른 물총 공격 |
| ★3 | 좌우 분사구와 상단 물방울 뿔을 모두 제거하고, 넓고 무거운 몸체 위에 초대형 중앙 물대포 하나만 생김 | 중앙 대구경 주포를 사용하는 공성 포격형 공격 |

- ★2 파일은 `unit_watergun_slime_star2.png`, ★3 파일은 `unit_watergun_slime_star3.png`를 사용한다.
- ★3 중앙 물대포는 분리된 금속 무기가 아니라 슬라임 몸체에서 자란 시안색 젤리 포신으로 표현한다. 포구는 몸체보다 먼저 읽힐 만큼 크고, 두꺼운 밝은 테두리와 짙은 내부로 압력을 표현한다.
- ★3은 측면 분사구가 0개다. 몸체를 더 넓고 무겁게 만들고, 눈썹 픽셀과 짧은 일자 입으로 결연하지만 귀여운 표정을 사용한다.
- 현재 전투 로직은 빠른 단일 투사체 역할을 유지한다. 대형 포구는 우선 성급 실루엣 차이를 위한 시각 요소이며 다중 발사는 별도 기획 확정 전 추가하지 않는다.
- 기존 ★2 한쪽 분사구 이미지는 2026-07-19에 좌우 대칭 분사구형으로 재생성·교체했다.

### 초록 슬라임 성급별 진화

| 성급 | 외형 | 이동·공격 방향 |
|---|---|---|
| ★1 | 얼굴과 식물 장식이 없는 밝은 초록 젤리 몸체 | 자연 속성 투사체로 지속 피해 부여 |
| ★2 | ★1 젤리 몸체에 작은 눈·입과 뒤쪽으로 부드럽게 휘어진 큰 잎 1장이 돋음 | 큰 잎을 흔들어 씨앗·잎날 형태의 자연 투사체 발사 |
| ★3 | 넓고 낮아진 중량형 젤리 몸체 위에 거대한 꽃 1송이와 넓은 짙은 초록 잎 여러 장이 자람 | 꽃가루·덩굴 에너지를 응축한 강한 자연 투사체 발사 |

- ★2 파일은 `unit_green_slime_star2.png`, ★3 파일은 `unit_green_slime_star3.png`를 사용한다.
- ★2는 첨부 이미지에서 `머리 위 큰 잎 하나`, `가볍고 친근한 초식형 인상`만 참고한다. 네 발 동물 체형·목 장식·꼬리·큰 붉은 눈은 복제하지 않고 둥근 슬라임 몸체를 유지한다.
- ★3은 두 번째 첨부 이미지에서 `낮고 묵직한 체형`, `등 위의 거대한 꽃`, `꽃을 둘러싼 넓은 잎`만 참고한다. 동물 얼굴·귀·다리·발톱·등껍질은 사용하지 않는다.
- ★3 꽃은 분홍·코랄 계열의 단일 큰 꽃으로 만들고, 3~5장의 넓은 잎이 뒤에서 받치는 독자적인 실루엣으로 구성한다.
- 전투 로직은 기존 자연 속성 원거리 투사체와 지속 피해를 유지한다. 잎·꽃가루·덩굴 표현은 우선 시각적 진화 요소이며 별도 소환수나 설치물은 추가하지 않는다.
- 현재 ★2 이미지는 얼굴과 내부 코어만 있고 잎이 없으므로 새 확정안에 맞춰 재생성한다.

### 3.1 ★2 공통 생성 프롬프트

각 기본형 이미지를 Image 1 정체성 레퍼런스로 사용하고 `{SLIME_NAME}`, `{EVOLUTION_CHANGE}`, `{CHROMA_KEY}`만 교체한다. 초록 슬라임은 몸체와 겹치지 않도록 `#FF00FF`, 나머지는 `#00FF00`을 사용한다. 주먹·물총·초록 슬라임 ★2는 팔다리·분사구·잎 예외가 있으므로 각각의 성급별 전용 지시를 우선한다.

```text
Use case: identity-preserve
Asset type: production-ready Unity 2D mobile tower-defense character sprite,
rank-two evolved ally slime

Input image:
- Image 1 is the exact base-form identity, body proportions, role silhouette,
  palette, pixel density, outline thickness, camera, scale, and framing reference.

Primary request:
Create one new rank-two evolved {SLIME_NAME} idle sprite derived from Image 1.
This is the first evolution where a face appears.
Preserve the same species and role identity.

Evolution change:
{EVOLUTION_CHANGE}
Add exactly two tiny dark square pixel eyes and one tiny simple dark neutral smile.
No eyebrows, eyelashes, nose, teeth, tongue, blush, or complex expression.

Identity and scale invariants:
Keep the permanent core slime body at the same visual scale, width,
ground-contact baseline, front-facing camera, and framing as Image 1.
Unity applies the rank size increase separately; do not enlarge the whole sprite.
Change only the simple face and the specified role evolution details.

Scene/backdrop:
Perfectly flat solid {CHROMA_KEY} chroma-key background for later removal.
No shadow, gradient, texture, reflection, floor, or lighting variation.
Do not use {CHROMA_KEY} inside the subject.

Style/medium:
Strict authentic 16-bit retro RPG pixel art matching Image 1,
with crisp hard square pixels, chunky clusters, and a dark outline.

Composition/framing:
Exactly one front-facing full-body slime, centered, idle pose,
generous even padding, square canvas, no cropped edges.

Constraints:
No star symbol, rank number, text, UI, watermark, sheet, extra character,
crown, clothing, armor, detached prop, cast shadow, large effect, particles,
aura, floating ornament, arms, hands, legs, or feet.
```

### 3.2 주먹 슬라임 ★2·★3 최종 프롬프트

★2는 ★1 이미지를 정체성 레퍼런스로 사용해 생성한 뒤, 첫 시안의 주먹이 지나치게 커서 `정밀 오브젝트 수정` 기법으로 팔과 주먹만 축소했다.

```text
Use case: precise-object-edit
Input images: Image 1은 첫 ★2 시안, Image 2는 정확한 ★1 몸체 기준.
Primary request: Image 1의 팔과 주먹만 수정한다. 양팔을 훨씬 짧게,
양쪽 주먹을 훨씬 작게 만들어 ★1 젤리 몸체의 좌우 하단에 밀착한다.
★1의 둥근 몸체가 주 실루엣이어야 하며 전체 폭은 몸체보다 조금만 넓다.
Preserve: 회청색 젤리 팔레트, 1px 어두운 외곽선, 크림색 하이라이트,
작은 사각 눈 2개, 단순한 미소, 정면 시점, 16-bit 픽셀 아트.
Constraints: 정확히 양팔·주먹 2개, 다리·근육질 몸통·장비·이펙트 없음,
단일 스프라이트, 평면 #00FF00 크로마키 배경.
```

★3은 최종 ★2와 ★1을 함께 정체성 레퍼런스로 사용했다.

```text
Use case: identity-preserve
Primary request: 같은 주먹 슬라임이 근육질 젤리 전사로 최종 진화한다.
Subject: ★2의 눈·입과 양팔을 유지하고, 넓은 어깨와 굵은 근육질 젤리 팔,
단순한 가슴 윤곽, 정확히 두 개의 짧고 굵은 다리와 둥근 발을 추가한다.
Silhouette: 큰 둥근 슬라임 머리·몸체, 넓은 어깨, 짧은 몸통과 다리.
사람이 아니라 젤리로 이루어진 귀엽고 단단한 근육 슬라임으로 읽혀야 한다.
Style: ★1·★2와 같은 회청색 팔레트와 strict 16-bit RPG pixel art.
Constraints: 인간 피부·현실적 근육·옷·장비·추가 팔다리·이펙트 없음,
정면 전신 단일 스프라이트, 평면 #00FF00 크로마키 배경.
```

### 3.3 물총 슬라임 ★2·★3 생성 프롬프트 및 개선 이력

초기 v1의 ★2는 ★1의 몸체·물방울 뿔·한쪽 분사구와 기존 ★2의 얼굴을 함께 정체성 레퍼런스로 사용했다.

```text
Use case: identity-preserve
Primary request: 같은 물총 슬라임의 ★2 진화형을 만든다.
Subject: ★1의 둥근 시안색 젤리 몸체와 상단 물방울 뿔을 유지하고,
오른쪽 분사구를 좌우 반전한 같은 크기의 짧은 분사구를 왼쪽에도 추가한다.
작은 사각 눈 2개와 단순한 미소 1개를 유지한다.
Silhouette: 본체가 중심이며 좌우 분사구는 상단 뿔보다 작고 서로 대칭이다.
Style: 기존 이미지와 같은 dark navy 1px outline의 strict 16-bit pixel art.
Constraints: 상단 물방울 뿔 1개, 좌우 분사구 정확히 2개.
팔·다리·상단 물대포·물줄기·투사체·장비 없음. 평면 #00FF00 크로마키.
```

초기 v1의 ★3은 당시 ★2를 정체성 레퍼런스로 사용해 상단 요소만 진화시켰다.

```text
Use case: identity-preserve
Primary request: ★2 물총 슬라임의 상단 물방울 뿔을 완전히 제거하고,
그 자리를 몸체에서 자란 짧고 굵은 중앙 젤리 물대포로 교체한다.
Subject: ★2의 얼굴과 좌우 대칭 분사구는 그대로 유지한다.
중앙 물대포는 양쪽 분사구보다 크며, 위쪽으로 약간 기울어
짙은 파란색 원형 포구와 밝은 시안색 테두리가 보이게 한다.
Silhouette: 둥근 본체가 중심이고 상단 대구경 포구가 ★3 핵심 표식이다.
Constraints: 중앙 물대포 정확히 1개, 좌우 분사구 정확히 2개.
기존 물방울 뿔·금속 부품·호스·물탱크·물줄기·이펙트 없음.
정면 단일 스프라이트, 평면 #00FF00 크로마키.
```

#### 2026-07-19 사용자 피드백 반영 v2

초기 ★2는 128px 가공 후 분사구가 ★1보다 작아 보였고, 초기 ★3은 작은 중앙 포구와 남아 있는 좌우 분사구 때문에 최종 진화의 힘이 부족했다. 최종본은 다음 수정 프롬프트를 사용한다.

```text
★2 precise-object-edit:
현재 좌우 분사구만 수정한다. 각 분사구를 기존의 약 1.7배로 키우고,
★1 오른쪽 분사구 이상으로 굵고 길게 만든다. 몸체 좌우에 같은 높이로 붙은
대형 시안색 젤리 포신과 짙은 포구를 정확히 2개 유지한다.
본체·상단 물방울 뿔·눈·미소·팔레트·픽셀 스타일은 변경하지 않는다.
```

```text
★3 precise-object-edit:
좌우 분사구와 상단 물방울 뿔을 모두 제거한다.
상단 중앙에 몸체 높이와 폭의 약 절반을 차지하는 초대형 젤리 물대포 하나를 만든다.
포신은 넓은 결합부, 여러 단계의 굵은 시안색 링, 거대한 짙은 남색 원형 포구를 가진다.
몸체는 더 넓고 무겁게 만들며 하단 압력 밴드를 강화한다.
표정은 작은 각진 눈썹·좁아진 눈·짧은 일자 입의 결연한 얼굴로 변경한다.
측면 포구·두 번째 대포·금속·등껍질·팔다리·물줄기·이펙트는 금지한다.
첨부 이미지는 대구경 포신과 무거운 실루엣의 분위기만 참고하고 캐릭터 디자인은 복제하지 않는다.
```

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
