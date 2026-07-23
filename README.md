# Isekai Slime Summoner

> 룰렛으로 슬라임을 소환하고 자유롭게 배치·머지해, 중앙의 소환사를 고블린 군단으로부터 지키는 모바일 디펜스 게임

NAN 2026 (NHN Game × AI Hackathon) 사전 과제를 위해 개발 중인 1인 Unity 프로젝트입니다.

장르는 **자유 배치 소환 디펜스 × 로그라이크 × 키우기**이며, AI는 코드·에셋·밸런싱을 만드는 개발 도구로만 활용합니다. 런타임 LLM API는 사용하지 않습니다.

![Isekai Slime Summoner 플레이 화면](Assets/Screenshots/dopamine_hud_playmode.png)

## 핵심 플레이

- 용병 계약서를 사용해 해금된 8종의 슬라임 중 하나를 무작위로 소환합니다.
- 슬라임을 필드에 자유롭게 배치하고, 같은 종류·성급 2개를 겹쳐 ★1 → ★2 → ★3으로 머지합니다.
- 중앙의 소환사와 슬라임 군단으로 여러 방향에서 몰려오는 고블린 웨이브를 막습니다.
- 무·화염·빙결·자연 속성 상성과 유닛별 공격 방식, 지원 오라를 조합합니다.
- 연속 처치 콤보로 오버드라이브 게이지를 채워 메테오 폭주를 발동합니다.
- 런 중 강화·행상인·유물과 런 밖 소환사 레벨·특성·장비·몬스터 도감을 성장시킵니다.

## 조작

- **소환**: 하단 소환 탭에서 계약서 1장을 사용합니다.
- **배치**: 벤치의 슬라임 카드를 필드로 드래그합니다.
- **재배치·머지**: 필드의 슬라임을 드래그해 옮기거나 같은 종류·성급의 슬라임 위에 놓습니다.
- **소환사 공격**: 필드의 적 또는 공격 방향을 터치합니다.
- **액티브 스킬**: 우하단 스킬 버튼을 누르고 목표 지점을 지정합니다.
- **성장·정보**: 하단 탭과 도감·행상인 모달을 사용합니다.

## 기술 스택

- Unity `6000.3.5f2`
- Universal Render Pipeline 2D
- Unity Input System
- UI Toolkit + 월드 `SpriteRenderer`
- ScriptableObject 기반 웨이브·유닛·강화 데이터
- 몬스터·소환수·투사체 오브젝트 풀링
- Android APK, Portrait, IL2CPP, ARM64
- 패키지 ID: `com.wonucode.crossdefense`

## 실행 방법

### 준비 사항

- [Git LFS](https://git-lfs.com/)
- Unity Hub
- Unity `6000.3.5f2`
- Android 빌드 시 Unity Android Build Support, SDK, NDK, OpenJDK

### 에디터 실행

```bash
git clone https://github.com/Leewonwoo/isekai-slime-summoner.git
cd isekai-slime-summoner
git lfs pull
```

Unity Hub에서 저장소 폴더를 연 뒤 [SampleScene.unity](Assets/Scenes/SampleScene.unity)를 열고 Play를 실행합니다.

### Android APK 빌드

Unity의 **File > Build Profiles**에서 Android를 선택해 빌드할 수 있습니다. 프로젝트에는 IL2CPP·ARM64와 세로 화면 설정이 적용되어 있습니다.

Windows 명령줄 검증 빌드는 다음 에디터 메서드를 사용합니다.

```powershell
& "<Unity.exe>" `
  -batchmode -nographics `
  -projectPath "<repository-path>" `
  -executeMethod CrossDefense.Editor.AndroidFeatureBuildHarness.BuildFromCommandLine `
  -logFile "Logs/android-build.log"
```

결과 APK는 `Builds/IsekaiSlimeSummoner-feature-smoke.apk`에 생성됩니다.

## 프로젝트 구조

```text
Assets/
  Art/          # 게임에서 사용하는 가공 완료 스프라이트
  Data/         # ScriptableObject 인스턴스
  Scenes/       # Unity 씬
  Scripts/
    Core/       # 게임·웨이브·성장·경제 흐름
    Data/       # ScriptableObject 정의
    UI/         # UI Toolkit 컨트롤러
    Units/      # 소환사·슬라임·고블린·투사체
  Tests/        # EditMode 테스트
  UI/
    USS/        # 디자인 토큰과 컴포넌트 스타일
    UXML/       # HUD·필드 오버레이·하단 패널·모달
ArtSource/      # AI 생성 이미지 원본
docs/           # 스펙, AI 활용 기록, 에셋 대장
tools/          # 에셋 가공 도구
```

## 프로젝트 문서

- [전체 게임 스펙](docs/SPEC.md) — 설계와 범위의 단일 기준 문서
- [UI 가이드라인](docs/ui-guidelines.md)
- [MVP 구현 체크리스트](docs/mvp-implementation-checklist.md)
- [AI 활용 기록](docs/ai-usage-log.md)
- [AI 에셋 대장](docs/asset-ledger.csv)
- [프롬프트 개선 사례](docs/prompt-cases/)
- [원본 에셋 관리 규칙](ArtSource/README.md)

## AI 활용 원칙

- 코드·이미지·밸런싱·문서 작업에 AI를 개발 도구로 활용합니다.
- 생성 에셋은 원본, 가공본, 프롬프트, 사용 도구와 라이선스 정보를 함께 기록합니다.
- 실패한 프롬프트와 개선 결과를 `docs/prompt-cases/`에 보존합니다.
- 런타임 LLM 호출은 비용과 심사 안정성을 위해 프로젝트 범위에서 제외합니다.

## 개발 상태

개발 기간은 2026-07-11부터 2026-08-10까지입니다. 현재 주 빌드 타깃은 Android APK입니다. 대회 원 제출 요건의 브라우저 플레이와 APK 허용 여부 차이는 최종 접수 요강에서 확인해야 합니다.
