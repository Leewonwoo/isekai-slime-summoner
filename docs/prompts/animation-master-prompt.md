# 캐릭터 애니메이션 공통 마스터 프롬프트

소환사·슬라임·고블린의 3×3 9프레임 시트에 공통으로 적용하는 제작 기준이다. 개별 문서에는 캐릭터 정체성과 동작 설명만 남기고, 프레임 정렬·크기·루트 앵커·Unity 슬라이스 규칙은 이 문서를 단일 기준으로 사용한다.

## 1. 참조 이미지 우선순위

1. **Image 1 — 정체성 기준**: 승인된 단일 정면 스프라이트. 얼굴·몸 비율·색·장비·픽셀 밀도·외곽선을 고정한다.
2. **Image 2 — 정렬 기준**: 위치와 크기가 검수된 3×3 시트 또는 기준 프레임. 셀 안의 몸체 위치·루트 앵커·여백을 고정한다.
3. **Image 3 — 동작 기준(선택)**: 러프 콘티나 포즈 참고. 동작과 타이밍만 가져오고 캐릭터 디자인·비율·색·카메라는 가져오지 않는다.

흔들림이나 크기 오류가 있는 기존 시트는 Image 2로 재사용하지 않는다. 아직 승인된 정렬 시트가 없다면 Image 2를 생략하고 Image 1과 아래 좌표 규칙으로 첫 시트를 만든 뒤, 검수 통과한 결과만 이후 Image 2로 사용한다.

## 2. 공통 불변값

- 최종 캔버스: **384×384px**
- 배열: **정확히 3열 × 3행, 총 9프레임**
- 셀: **128×128px**, 간격·테두리·라벨 없음
- 순서: 왼쪽→오른쪽, 위→아래
- 셀 로컬 좌표는 왼쪽 위가 `(0, 0)`이며, 기본 루트 앵커는 `(64, 112)`
- 모든 프레임의 카메라 각도·줌·픽셀 밀도·캔버스 원점·셀 경계·캐릭터 기준 크기를 고정
- 각 프레임을 불투명 외곽선 전체나 무기·주먹·장식의 바운드로 따로 자동 중앙 정렬하지 않음
- 동작에 필요한 관절·몸체 변형만 허용하고, 캐릭터 전체의 일괄 확대·축소·평행 이동은 금지
- 첫 프레임과 마지막 프레임은 idle 기준 자세·크기·루트 위치가 일치해야 함

`(64, 112)`는 **그림 내용의 루트 위치**다. Unity Sprite Editor의 피벗 좌표와 혼동하지 않는다.

## 3. 캐릭터별 앵커 프로필

### `HUMANOID` — 소환사·고블린

- 루트는 접지한 두 발 사이의 중앙점이다.
- 한 발을 드는 프레임은 지지발과 골반 중심을 이용해 원래 루트를 유지한다.
- 지팡이·몽둥이·팔·귀·머리카락은 정렬이나 크기 계산에서 제외한다.
- 무기가 비대칭으로 길어지거나 각도가 바뀌어도 몸통과 발 위치는 밀리지 않는다.
- idle의 의도된 무릎 굽힘·상하 바운스는 허용하되, 발 루트의 좌우 이동은 금지한다.

### `SLIME` — 모든 슬라임

- 루트는 승인된 idle 자세에서 바닥에 닿는 몸체 밑면의 중앙점이며, 이후 프레임에서도 움직이지 않는 **가상 원점**으로 사용한다.
- 기준 크기는 영구적인 **주 몸체 코어**로 계산한다.
- 공격 중 생기는 팔·주먹·물줄기·폭발 돌기와 불꽃·얼음·퓨즈·chevron 같은 역할 장식은 정렬 및 크기 계산에서 제외한다.
- squash/stretch는 몸체 내부 변형으로 허용하지만 카메라 줌이나 캐릭터 전체 스케일 변경으로 표현하지 않는다.
- 공중에 뜨는 포잉 프레임은 몸체만 가상 루트 위로 소폭 이동할 수 있다. 셀 원점과 루트를 다시 계산하거나 재중앙 정렬하지 않고, 착지 프레임은 정확히 같은 밑면 중앙으로 돌아온다.

## 4. 복사해서 쓰는 공통 마스터 프롬프트

아래에서 대괄호 변수와 앵커 프로필 블록만 교체한다. 크로마키 색은 개별 에셋 문서의 값을 따른다.

```text
Use case: identity-preserve
Asset type: production-ready 2D pixel-art animation sprite sheet for Unity

Input images:
- Image 1 is the exact character identity and scale reference.
- Image 2, when supplied, is the approved animation alignment reference.
  Its cell composition, core-body scale, root position, padding,
  outline thickness, pixel density, and color palette are mandatory.
- Image 3, when supplied, is a motion reference only.
  Use it only for action and timing. Do not copy its character design,
  proportions, colors, camera, rendering, or layout.

Character identity:
Preserve the exact identity, body proportions, permanent silhouette,
equipment, palette, pixel-art rendering, and outline thickness from Image 1.
Do not redesign or reinterpret the character between frames.

Primary request:
Create a [ANIMATION_NAME] animation of the same character.
Produce exactly 9 consecutive frames in one strict 3 by 3 layout.
Read the frames left-to-right and top-to-bottom.
Each final cell represents exactly 128 by 128 pixels in a 384 by 384 sheet.
The result must be suitable for direct frame-by-frame playback in Unity.

Global alignment:
- Use the exact same front-facing camera angle and camera zoom in all frames.
- Use the exact same core-body scale and pixel density in all frames.
- Apply the [ANCHOR_PROFILE] root-anchor rules below.
- In every cell, keep the root at x=64 and y=112,
  measured from the top-left corner of that cell.
- Keep the pelvis or main body core aligned to that root.
- Do not independently auto-center each frame.
- Do not center or scale from the entire changing silhouette,
  weapon bounds, temporary limbs, attack extensions, or effects.
- Preserve identical canvas origin, cell boundaries, and neutral-pose padding.
- No camera movement, zoom, rotation, perspective change,
  unintended translation, character-wide scaling, or cropping.

Anchor profile:
[ANCHOR_PROFILE_BLOCK]

Animation sequence:
[FRAME_DESCRIPTIONS]

Animation transition:
[LOOP_OR_END_REQUIREMENTS]

Allowed deformation:
[DEFORMATION_RULES]

Style:
Crisp hard-edged 2D pixel art. Preserve the exact rendering style,
pixel density, palette logic, and outline thickness of Image 1.
No soft painting, 3D rendering, blurred edges, antialiasing,
inconsistent outline thickness, or newly invented details.

Scene/backdrop:
One continuous perfectly flat solid [CHROMA_KEY_HEX] chroma-key background.
The background must be one uniform color with no shadow, gradient,
texture, reflection, floor plane, lighting variation, or contact shadow.

Constraints:
Change only the pose and controlled deformation required by the animation.
No extra characters, objects, text, labels, borders, grid lines,
watermarks, scenery, particles, motion trails, or effects unless the
animation-specific prompt explicitly requests a small attached effect.
Keep every body part and allowed prop inside its own cell.
```

## 5. 프로필 삽입 블록

### `HUMANOID`

```text
Use the midpoint between the grounded feet as the root anchor.
If one foot lifts, preserve the original root using the support foot
and pelvis center. Keep the body and pelvis aligned to the root.
Exclude staff, club, weapon, hair, ears, and moving arms from alignment
and scale calculations. A changing weapon angle must never shift the body.
```

### `SLIME`

```text
Use the center of the approved idle body's ground-contact base as a fixed
virtual root anchor. Never recalculate this root from a deformed frame.
Use only the permanent core slime body to judge identity and base scale.
Exclude temporary arms, fists, jets, attack extensions, particles,
and role crests from alignment and scale calculations.
Squash and stretch may deform the core locally, and an intentional airborne
pose may lift the body slightly above the fixed virtual root. Do not change
camera zoom, apply uniform character-wide scaling, or recenter the cell.
Return the contact base exactly to the approved idle position on landing.
```

## 6. 프로젝트용 동작 변수

### 소환사 idle

```text
[ANIMATION_NAME]
gentle summoner idle bounce

[ANCHOR_PROFILE]
HUMANOID

[FRAME_DESCRIPTIONS]
Frame 1: exact neutral standing pose.
Frame 2: begin a very small knee bend.
Frame 3: lowest controlled crouch; feet and root remain fixed.
Frame 4: rise toward neutral.
Frame 5: exact neutral pose.
Frame 6: subtle upward stretch without lifting the feet.
Frame 7: settle downward.
Frame 8: rise back toward neutral.
Frame 9: exact neutral pose matching Frame 1.

[LOOP_OR_END_REQUIREMENTS]
Create a seamless loop. Frame 9 must transition into Frame 1 with no jump.

[DEFORMATION_RULES]
Only knees, pelvis height, coat hem, hair tips, and the held staff may move
by a few pixels. No walking, lateral sway, foot sliding, or staff-hand separation.
```

### 소환사 기본 공격

```text
[ANIMATION_NAME]
small in-place staff raise attack

[ANCHOR_PROFILE]
HUMANOID

[FRAME_DESCRIPTIONS]
Frame 1: exact approved idle pose.
Frame 2: small knee-bend anticipation.
Frame 3: begin raising the staff slightly.
Frame 4: staff reaches a modest raised angle.
Frame 5: hold the highest pose briefly.
Frame 6: begin lowering the staff.
Frame 7: staff and upper body return near idle.
Frame 8: settle the body.
Frame 9: exact approved idle pose matching Frame 1.

[LOOP_OR_END_REQUIREMENTS]
This is non-looping. Frames 1 and 9 must match the approved idle pose,
scale, root, and body position exactly.

[DEFORMATION_RULES]
Move only the knees, upper body, staff arm, and staff angle by a small amount.
Keep both hands attached correctly. Do not draw a projectile, spell burst,
large magic effect, full swing, body turn, or foot movement.
```

슬라임 이동·공격과 고블린 달리기·공격의 `[FRAME_DESCRIPTIONS]`는 각 개별 프롬프트 문서를 사용하고, 나머지 공통 문장은 이 마스터를 유지한다.

## 7. 결과 교정용 프롬프트

포즈와 디자인은 괜찮지만 위치·크기만 흔들릴 때는 새로 재해석시키지 말고 아래 문장으로 편집한다. 프로필에 맞게 `feet midpoint` 또는 `ground-contact base center`를 선택한다.

```text
Change only the frame alignment and core-body scale.
Keep the character artwork, poses, timing, colors, and design unchanged.

Align every frame using the [ROOT_ANCHOR_DESCRIPTION],
not the overall silhouette and not the weapon, crest, fist,
or temporary attack-extension bounds.
Keep that root anchor at x=64 and y=112 in every 128 by 128 cell,
measured from the top-left corner of the cell.
Normalize only unintended core-body scale variation.
Do not redesign, redraw, re-pose, crop, or independently auto-center frames.
```

## 8. 후처리·Unity 검수 규칙

- 생성 결과가 384×384가 아니면 먼저 **시트 전체를 한 번에** 정규 캔버스에 맞춘다.
- 애니메이션 프레임별 알파 바운딩 크롭·자동 트림·자동 중앙 정렬을 하지 않는다.
- Unity Sprite Editor는 `Grid by Cell Size`의 **128×128**, Offset 0, Padding 0으로 슬라이스한다.
- 9개 Sprite Rect는 모두 정확히 128×128이어야 한다.
- Unity 피벗은 같은 캐릭터의 모든 시트·프레임에서 동일하게 유지한다. 현재 제작 기준은 `Center (0.5, 0.5)`이며, 루트 `(64, 112)`는 이미지 안의 정렬 기준이다.
- PPU는 캐릭터별 문서를 따른다. 현재 소환사 시트 140, 슬라임 200, 적 고블린 220이며, 같은 캐릭터의 idle·이동·공격 사이에서는 절대 바꾸지 않는다.
- 공통 임포트: Sprite Mode Multiple, Filter Point, Compression None, Mipmap Off.
- 프레임 1과 9를 반투명 오버레이해 루트·몸체 크기·중립 자세가 겹치는지 확인한다.
- 생성 모델이 셀마다 다른 위치에 배치한 경우 [normalize_animation_sheet.py](../../tools/normalize_animation_sheet.py)로 **셀 크기는 유지한 채 셀 내부의 루트만 평행 이동**한다. 지팡이·공격 돌출부가 아닌 몸체 밀집 영역을 기준으로 `(64, 112)`에 맞춘다.
- 프롬프트의 좌표 문장만으로 픽셀 정렬을 보장할 수 없으므로, 위 고정 그리드와 오버레이 검수를 통과한 시트만 Image 2 승인을 부여한다.
