# 배포와 스토어 등록

## GitHub 배포

`main` push와 PR에서 Windows 빌드·테스트·ZIP 패키징을 실행합니다. Actions 실행 화면의 `plugin-package`에서 ZIP을 받을 수 있습니다.

릴리스할 때는 `plugin.json`과 플러그인 csproj의 버전을 맞추고 변경을 커밋한 뒤 해당 버전의 태그를 올립니다.

```powershell
git push origin main
git tag v1.0.0
git push origin v1.0.0
```

태그와 `plugin.json` 버전이 일치하고 빌드·테스트가 통과하면 Actions가 GitHub Release를 만들고 설치 ZIP을 첨부합니다. 별도의 개인 토큰은 필요하지 않습니다. 자동 검증은 실제 입력이나 PC 종료를 실행하지 않습니다. 실제 사용자 환경 검증은 별도로 진행하세요.

## 스토어 등록

1. GitHub Release의 ZIP이 다운로드되는지 확인합니다.
2. `docs/store/DoAction-D5B032E0FE594208B0687010889D6450.json`의 버전과 URL이 릴리스와 일치하는지 확인합니다.
3. https://github.com/Flow-Launcher/Flow.Launcher.PluginsManifest 를 Fork합니다.
4. 등록 JSON을 Fork한 저장소의 `plugins/`에 추가하고 공식 저장소의 기본 브랜치로 PR을 제출합니다.

등록 파일의 `MinimumAppVersion`은 현재 확인한 Flow 2.1.4로 지정했습니다. 소스·릴리스 공개와 스토어 승인은 별도입니다.

공식 안내: https://github.com/Flow-Launcher/Flow.Launcher.PluginsManifest#how-to-submit-your-plugin
