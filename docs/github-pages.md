# GitHub Pages WebGL 배포

GitHub Pages는 Unity 프로젝트나 Android APK를 직접 실행하지 않는다. 이 저장소는 Unity가 생성한 WebGL 정적 파일을 `PagesBuild/`에 보관하고, GitHub Actions가 해당 폴더만 Pages에 배포한다.

## 로컬 빌드

Unity 6000.3.5f2가 설치된 Windows 환경에서 저장소 루트 기준으로 실행한다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.5f2\Editor\Unity.exe' `
  -batchmode -nographics `
  -projectPath (Get-Location).Path `
  -buildTarget WebGL `
  -executeMethod CrossDefense.Editor.GitHubPagesBuild.BuildFromCommandLine `
  -logFile 'Temp/github-pages-build.log'
```

빌드 스크립트는 WebGL 출력에만 Gzip과 Decompression Fallback을 적용하고 작업이 끝나면 기존 PlayerSettings 값을 복원한다. 프로젝트의 기본 Android 빌드 설정은 변경하지 않는다.

## 배포

`PagesBuild/`와 `.github/workflows/deploy-pages.yml`을 `main`에 푸시하면 `Deploy WebGL to GitHub Pages` 워크플로가 실행된다. 수동 재배포는 GitHub Actions 화면의 `workflow_dispatch`로 실행할 수 있다.

배포 주소: <https://leewonwoo.github.io/isekai-slime-summoner/>
