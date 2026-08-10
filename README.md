# Isekai Slime Summoner

<p align="center">
  <strong>소환하고, 배치하고, 합쳐서 살아남는 세로형 로그라이크 디펜스</strong>
</p>

<p align="center">
  <img alt="Unity 6000.3.5f2" src="https://img.shields.io/badge/Unity-6000.3.5f2-000000?logo=unity">
  <img alt="C#" src="https://img.shields.io/badge/C%23-Game%20Logic-512BD4?logo=csharp">
  <img alt="Android" src="https://img.shields.io/badge/Android-APK-3DDC84?logo=android&logoColor=white">
  <img alt="WebGL" src="https://img.shields.io/badge/WebGL-Build%20Ready-F97316">
</p>

<p align="center">
  <a href="https://leewonwoo.github.io/isekai-slime-summoner/"><strong>🎮 WebGL 게임 바로 실행</strong></a>
  ·
  <a href="https://github.com/Leewonwoo/isekai-slime-summoner/releases/latest/download/IsekaiSlimeSummoner.apk">Android APK 다운로드</a>
  ·
  <a href="https://github.com/Leewonwoo/isekai-slime-summoner/releases/latest">릴리스 정보</a>
</p>

<p align="center">
  <img src="Assets/Screenshots/dopamine_hud_playmode.png" width="360" alt="Isekai Slime Summoner 전투 화면">
</p>

NAN 2026(NHN Game × AI Hackathon) 사전 과제를 위해 제작한 1인 Unity 프로젝트입니다. 룰렛으로 슬라임을 소환하고 자유롭게 배치·머지해 중앙의 소환사를 고블린 군단으로부터 지키는 게임입니다.

장르는 **자유 배치 소환 디펜스 × 로그라이크 × 키우기**입니다. AI는 코드·에셋·밸런싱·문서 제작을 돕는 개발 도구로만 사용하며, 게임 실행 중 LLM API를 호출하지 않습니다.

## 게임 실행

**설치 없이 플레이하려면 [GitHub Pages WebGL 버전](https://leewonwoo.github.io/isekai-slime-summoner/)을 이용하는 것을 권장합니다.** PC와 모바일 브라우저에서 링크를 열면 바로 실행할 수 있습니다.

Android APK는 [GitHub Releases](https://github.com/Leewonwoo/isekai-slime-summoner/releases/latest)에서도 받을 수 있지만, Google Play 외부에서 배포되는 APK 특성상 일부 기기에서는 Google Play Protect의 **앱 차단 또는 기기 보호 경고**가 표시되거나 설치가 완료되지 않을 수 있습니다. APK 다운로드·설치가 되지 않는 경우에는 기기 설정을 변경하지 말고 위 WebGL 링크로 접속해 주세요.

> 권장 실행 경로: **WebGL 바로 실행** → APK 설치가 꼭 필요한 경우에만 Release APK 사용

## 게임 핵심 루프

`계약서 획득` → `슬라임 무작위 소환` → `필드 배치·2머지` → `고블린 웨이브 방어` → `런 강화 선택` → `영구 성장`

- 해금된 8종의 슬라임 중 하나를 룰렛으로 소환합니다.
- 슬라임을 필드에 자유롭게 배치하고 같은 종류·성급 2개를 합쳐 ★1 → ★2 → ★3으로 성장시킵니다.
- 무·화염·빙결·자연 속성 상성과 근접·원거리·광역·지원 역할을 조합합니다.
- 연속 처치 콤보로 오버드라이브 게이지를 채워 메테오 폭주를 발동합니다.
- 특성 3택, 행상인, 전리품, 신물과 소환사 전투 빌드로 매 런을 다르게 구성합니다.
- 런 밖에서는 소환사 레벨, 슬라임 강화, 장비와 도감을 영구 성장시킵니다.

## 플레이 화면

<table>
  <tr>
    <td align="center"><img src="Assets/Screenshots/summon_roulette_skin_applied.png" width="300" alt="소환 룰렛"><br><sub>슬라임 소환 룰렛</sub></td>
    <td align="center"><img src="Assets/Screenshots/catalog_icons_field.png" width="300" alt="전투 필드와 카탈로그 아이콘"><br><sub>전투 필드와 스킬 UI</sub></td>
    <td align="center"><img src="Assets/Screenshots/summoner_tab_profile.png" width="300" alt="소환사 성장 탭"><br><sub>소환사 영구 성장</sub></td>
  </tr>
</table>

## 조작

| 행동 | 조작 방법 |
|---|---|
| 소환 | 하단 소환 탭에서 계약서 1장을 사용합니다. |
| 배치 | 벤치의 슬라임 카드를 필드로 드래그합니다. |
| 재배치·머지 | 필드 유닛을 옮기거나 같은 종류·성급 유닛 위에 놓습니다. |
| 소환사 공격 | 필드의 적 또는 공격 방향을 터치합니다. |
| 액티브 스킬 | 우하단 스킬 버튼을 누른 뒤 목표 지점을 지정합니다. |
| 성장·정보 | 하단 5개 탭과 도감·행상인 화면을 사용합니다. |

## 기술 구성

| 구분 | 내용 |
|---|---|
| 엔진 | Unity `6000.3.5f2`, Universal Render Pipeline 2D |
| 언어 | C# |
| 입력·UI | Unity Input System, UI Toolkit, 월드 `SpriteRenderer` |
| 데이터 | ScriptableObject 기반 웨이브·유닛·성장·보상 카탈로그 |
| 런타임 | 몬스터·소환수·투사체 오브젝트 풀링, 규칙 기반 전투 AI |
| Android | Portrait, IL2CPP, APK (기기에 따라 Play Protect 설치 제한 가능) |
| Web | Unity WebGL, Gzip + Decompression Fallback, GitHub Pages 배포 |
| 패키지 ID | `com.nenestudio.isekaislimesummoner` |

## 시작하기

### 준비 사항

- [Git LFS](https://git-lfs.com/)
- Unity Hub
- Unity `6000.3.5f2`
- Android 빌드 시 Android Build Support, SDK, NDK, OpenJDK
- WebGL 빌드 시 WebGL Build Support

### 저장소와 LFS 에셋 받기

```bash
git clone https://github.com/Leewonwoo/isekai-slime-summoner.git
cd isekai-slime-summoner
git lfs pull
```

Unity Hub에서 저장소 폴더를 연 뒤 [`SampleScene.unity`](Assets/Scenes/SampleScene.unity)를 열고 Play를 실행합니다.

### Android APK 빌드

Unity의 **File > Build Profiles**에서 Android를 선택해 빌드할 수 있습니다. 명령줄 검증 빌드는 다음 에디터 메서드를 사용합니다.

```powershell
& "<Unity.exe>" `
  -batchmode -nographics `
  -projectPath "<repository-path>" `
  -executeMethod CrossDefense.Editor.AndroidFeatureBuildHarness.BuildFromCommandLine `
  -logFile "Logs/android-build.log"
```

결과 APK는 `Builds/IsekaiSlimeSummoner-feature-smoke.apk`에 생성됩니다.

### WebGL 빌드

프로젝트의 기본 Android 설정을 바꾸지 않고 명령 실행 중에만 WebGL 타깃으로 전환합니다.

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.5f2\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "<repository-path>" `
  -buildTarget WebGL `
  -executeMethod CrossDefense.Editor.GitHubPagesBuild.BuildFromCommandLine `
  -logFile "Temp/github-pages-build.log"
```

정적 결과물은 `PagesBuild/`에 생성됩니다. 자세한 배포 흐름은 [`docs/github-pages.md`](docs/github-pages.md)를 참고하세요.

## 검증

프로젝트에는 레거시 EditMode 테스트와 주요 화면·상태 전환을 확인하는 PlayMode 스모크 하네스가 포함되어 있습니다.

```powershell
# EditMode 테스트
& "<Unity.exe>" -batchmode -nographics `
  -projectPath "<repository-path>" `
  -executeMethod CrossDefense.Editor.EditModeTestHarness.RunFromCommandLine `
  -logFile "Temp/editmode-tests.log"

# PlayMode 스모크
& "<Unity.exe>" -batchmode -nographics `
  -projectPath "<repository-path>" `
  -executeMethod CrossDefense.Editor.FeaturePlayModeSmokeHarness.RunFromCommandLine `
  -logFile "Temp/playmode-smoke.log"
```

최근 확인 결과: **EditMode 133개 통과**, **PlayMode 스모크 통과**, **WebGL 브라우저 초기화 성공**.

## 프로젝트 구조

```text
Assets/
  Art/          # 게임에서 사용하는 가공 완료 스프라이트
  Audio/        # BGM과 전투 효과음
  Data/         # ScriptableObject 인스턴스
  Editor/       # 빌드·테스트·에셋 제작 도구
  Scenes/       # Unity 씬
  Scripts/
    Core/       # 런, 웨이브, 전투, 성장과 경제 흐름
    Data/       # ScriptableObject 및 카탈로그 정의
    UI/         # UI Toolkit 컨트롤러
    Units/      # 소환사, 슬라임, 고블린과 투사체
  Tests/        # EditMode 테스트
  UI/           # UXML 레이아웃과 USS 스타일
ArtSource/      # AI 생성 이미지 원본
PagesBuild/     # GitHub Pages용 WebGL 정적 빌드
docs/           # 스펙, 가이드, AI 활용 기록과 에셋 대장
tools/          # 이미지·오디오 가공 도구
```

## 문서

- [전체 게임 스펙](docs/SPEC.md) — 설계와 범위의 단일 기준 문서
- [UI 가이드라인](docs/ui-guidelines.md) — UI Toolkit 구조와 스타일 규칙
- [MVP 구현 체크리스트](docs/mvp-implementation-checklist.md) — 기능별 구현·검증 현황
- [GitHub Pages 배포](docs/github-pages.md) — WebGL 생성과 배포 방법
- [AI 활용 기록](docs/ai-usage-log.md) — 코드·에셋·밸런싱·사운드 작업 이력
- [AI 에셋 대장](docs/asset-ledger.csv) — 생성 도구, 프롬프트와 라이선스 기록
- [프롬프트 개선 사례](docs/prompt-cases/) — 실패 원인과 개선 과정
- [원본 에셋 관리 규칙](ArtSource/README.md)

## AI 활용 원칙

- AI는 코드·이미지·밸런싱·사운드·문서를 만드는 개발 보조 도구로 사용합니다.
- 생성 에셋은 원본, 가공본, 프롬프트, 사용 도구와 라이선스 정보를 함께 기록합니다.
- 실패한 프롬프트와 개선 결과는 `docs/prompt-cases/`에 보존합니다.
- 플레이 중에는 LLM이나 외부 생성형 AI API를 호출하지 않습니다.

## 개발 상태

- 개발 기간: `2026-07-11` ~ `2026-08-10`
- 개발 형태: 1인 개발
- 주 배포물: [GitHub Pages WebGL 버전](https://leewonwoo.github.io/isekai-slime-summoner/)
- 보조 배포물: Android APK (GitHub Releases)
- 현재 단계: 기능 동결 후 폴리싱·밸런싱·제출 문서 정리
