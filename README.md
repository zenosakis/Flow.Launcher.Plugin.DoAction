# Flow.Launcher.Plugin.DoAction

DoAction3의 자동 입력과 PC 종료 예약을 두 개의 독립 기능으로 제공하는 Windows용 플러그인입니다.

[릴리스 다운로드](https://github.com/zenosakis/Flow.Launcher.Plugin.DoAction/releases) · [빌드 상태](https://github.com/zenosakis/Flow.Launcher.Plugin.DoAction/actions) · [배포·스토어 등록 안내](docs/publishing.md)

## 설치와 시작

1. `Flow.Launcher.Plugin.DoAction-1.0.0.zip`을 압축 해제합니다.
2. 압축 해제한 폴더의 `Install.ps1`을 PowerShell에서 실행합니다. 기본 설치 위치는 `%APPDATA%\FlowLauncher\Plugins\Flow.Launcher.Plugin.DoAction-1.0.0`입니다.
3. Flow Launcher를 다시 시작하고 `da`를 입력합니다.
4. **DoAction 설정**에서 옵션을 저장한 후 **자동 입력 시작** 또는 **PC 종료/재부팅 예약**을 선택합니다.

Flow의 설정 → 플러그인 → DoAction에서도 같은 설정 화면을 제공합니다. 설치·Flow 재시작만으로 자동 입력이나 PC 예약이 실행되지는 않습니다.

포터블 Flow는 `Install.ps1 -PluginRoot '포터블 Flow의 UserData\Plugins'`로 실제 플러그인 경로를 지정하세요.

## 자동 입력

- 마우스 이동 / 클릭 / 이동 → 클릭 / 키보드 / 이동 → 키보드 / 이동 → 클릭 → 키보드
- 반복 간격 0.1~86400초, 시작 대기 1~60초
- 이동 방향·거리, 왼쪽/오른쪽/더블 클릭
- 무작위 영문·숫자, 최대 500자 문자열, Space/Enter/Tab/F15
- 전체화면 옵션: 모든 모니터를 덮고 주 모니터의 전용 입력 영역에서 동작

**실제 키보드의 아무 키나 누르면 이동·클릭·키보드 입력을 모두 종료하고 전체화면을 닫습니다. 자동 재개하지 않습니다.** 다시 `da` → 자동 입력 시작을 선택해야 합니다. 시작에 사용한 Enter 등 이미 눌린 키는 떼기를 기다린 후 대기 시간을 계산합니다. 시작 준비 중 새 키를 누르면 시작도 취소됩니다.

자동 생성된 키 입력은 정지 신호로 처리하지 않습니다. 중지 시 이미 전달된 문자·클릭을 취소할 수는 없으며, 다음 입력 전마다 취소를 확인합니다. 문자열은 짧게 나누어 전송하고 키 누름·뗌은 한 쌍으로 전송합니다.

전체화면을 끄면 **현재 활성 창에 입력**합니다. 대상 앱은 직접 선택하세요. Flow 창으로 돌아오거나 전체화면에서 다른 앱으로 전환하면 자동 동작이 종료됩니다. 화면 잠금·절전 시에도 중지합니다. `da` → 자동 입력 중지에서도 종료할 수 있습니다.

일반 권한의 Flow에서 관리자 권한 앱으로의 입력은 Windows 정책에 의해 차단될 수 있습니다. UAC 보안 화면이나 잠금 화면은 자동화 대상이 아닙니다. 다른 프로그램이 주입한 키 입력 역시 실제 키 입력으로 간주하지 않습니다. 키보드 감시 등록에 실패하면 자동 입력을 시작하지 않습니다.

## PC 종료·재부팅 예약

- PC 종료 또는 재부팅 중 하나를 선택합니다.
- 몇 분 후(1~10080분) 또는 현지 날짜·시각을 지정합니다.
- **예약 메뉴를 선택해야 활성화**됩니다. 설정 저장만으로 예약하지 않습니다.
- 등록 직후 안내에 작업 종류, 정확한 실행 시각(PC 현지 시각), 실행 전 안내 시간, 강제 종료 여부와 취소 방법을 표시합니다. `da`의 **상태** 항목에서 다시 볼 수 있습니다. 설정을 바꿔도 안내는 실제 등록된 예약 기준입니다.
- 동시에 한 개의 예약만 유지합니다. 예약 후 설정을 바꿔도 기존 예약 시각·작업은 유지됩니다.
- 실행 전 10~300초 안내(기본 60초). 예약 시간은 안내 시간보다 길어야 합니다.
- 안내 전에 자동 입력을 종료하고, 안내 창에서 취소할 수 있습니다. 창의 X도 예약 취소입니다.
- 강제 종료 기본 꺼짐. 저장하지 않은 앱이 있으면 Windows가 종료를 막을 수 있습니다.
- 강제 종료를 켜면 저장하지 않은 내용을 잃을 수 있습니다.

**자동 입력 중지와 예약 취소는 독립적입니다.** 실제 키를 눌러 자동 입력을 중지해도 PC 예약은 유지됩니다. `da` → PC 예약 취소로 예약을 취소하세요. Windows에 실제 종료 요청이 전달된 이후에는 플러그인에서 취소할 수 없습니다.

Flow가 실행 중인 동안만 유지되는 1회 예약이며, Flow 종료·절전 시 취소됩니다. 다시 실행해도 복구하지 않습니다. 10초를 초과하는 실행 공백이나 큰 시계 변경이 감지되면 갑작스러운 종료를 피하기 위해 예약을 만료 처리합니다. 화면 잠금만으로 PC 예약을 취소하지는 않습니다.

카운트다운은 플러그인 내부에서 처리합니다. 실제 명령은 `shutdown.exe /s /t 0` 또는 `/r /t 0`이며, 강제 종료를 직접 켠 경우에만 `/f`를 추가합니다. 다른 앱이나 Windows가 만든 종료 예약은 취소하지 않습니다.

## 개발·검증

- .NET 8 SDK, Windows, WPF
- Flow Launcher 2.1.4의 설치된 API 어셈블리에 대한 인터페이스 로드 호환성 확인
- `Flow.Launcher.Plugin` 4.7.0을 컴파일 참조로 사용하고, 실행 시 Flow가 제공하는 API를 사용합니다. API DLL을 배포물에 포함하지 않습니다.

```powershell
.\scripts\Build.ps1
dotnet run --project tests/DoAction.Tests -c Release -- artifacts/qa
```

테스트는 실제 키·클릭·PC 종료를 실행하지 않습니다. 입력 판별, 취소, 예약 만료·시각 검증, 강제 종료 명령 구성, WPF 설정 화면 렌더링을 검증합니다. 실제 앱 입력·다중 모니터 동작·최종 PC 종료는 사용 환경에서 별도 확인이 필요합니다.

실제 전체화면 입력 회귀 검증은 `dotnet run --project tests/DoAction.Tests -c Release -- --live`로 별도 실행합니다. 약 4초 동안 테스트 전용 전체화면에만 문자를 입력하고 클릭한 뒤 자동으로 닫습니다. PC 전원 작업은 실행하지 않습니다.

2026-09-22 수정: Flow 2.x의 비동기 검색창 숨김이 완료된 뒤 자동 입력을 시작하도록 변경하고 중복 숨김 요청을 제거했습니다. 입력 종료 후 창 정리는 UI 스레드에서 수행하며, 상태 변경은 Flow 로그에 기록합니다.

산출물: `artifacts/Flow.Launcher.Plugin.DoAction-1.0.0.zip`, `artifacts/qa/` 설정 화면 이미지.

## 구조

- `src/DoAction.Core`: 설정 검증, 실제 키 판별, 예약 상태
- `src/Flow.Launcher.Plugin.DoAction`: Flow 연동, WPF 설정·전체화면, 입력 감시·전송, 예약 안내
- `tests/DoAction.Tests`: 실제 PC 동작을 보내지 않는 검증 실행기
- `scripts`: 빌드·패키징·로컬 설치

## 참고

- [Flow 플러그인 개발](https://www.flowlauncher.com/docs/#/plugin-dev)
- [Windows 키보드 이벤트](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct)
- [Windows shutdown 명령](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/shutdown)

아이콘은 새로 생성한 커서·반복 화살표 디자인입니다. PNG 원본과 16~256px ICO를 `src/Flow.Launcher.Plugin.DoAction/Images`에 보관합니다. 생성 프롬프트는 [docs/icon-generation.md](docs/icon-generation.md)를 참고하세요. ICO 재생성: `./scripts/Export-Icon.ps1`.
