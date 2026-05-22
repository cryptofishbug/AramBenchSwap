# ARAM Bench Swap

**한국어** | [English](#english)

ARAM 벤치/공유풀 챔피언을 빠르게 수동으로 바꾸기 위한 Windows 유틸리티입니다. 로컬 League Client API(LCU)를 사용합니다.

## 기능

- 로컬 League Client의 챔피언 선택 세션을 감시합니다.
- ARAM 벤치/공유풀이 사용 가능할 때 작은 always-on-top 패널을 보여줍니다.
- 사용자가 챔피언 아이콘을 직접 클릭했을 때만 벤치 스왑 요청을 보냅니다.
- 챔피언 자동 선택, 자동 추천, 자동 우선순위 판단, 자동 스왑은 하지 않습니다.

실제로 보내는 요청은 다음 하나입니다.

```http
POST /lol-champ-select/v1/session/bench/swap/{championId}
```

## 중요 안내

이 프로젝트는 Riot Games와 무관한 비공식 개인 유틸리티입니다. LCU endpoint는 공식적으로 서드파티 앱에 안정성을 보장하는 API가 아니며, 클라이언트 업데이트로 언제든 변경될 수 있습니다.

사용은 본인 책임입니다. 이 앱은 직접 클릭 기반의 수동 스왑만 하도록 설계되었습니다. 자동으로 챔피언을 고르거나 선점하는 용도로 사용하지 마세요.

## 다운로드

일반 사용자는 GitHub Releases에서 최신 `AramBenchSwap-win-x64.zip`을 다운로드한 뒤 압축을 풀고 `AramBenchSwap.exe`를 실행하면 됩니다.

zip에는 다음 파일만 있으면 됩니다.

```text
AramBenchSwap.exe
AramBenchSwap.Core.dll
```

## 사용 방법

1. League of Legends 클라이언트를 실행합니다.
2. `AramBenchSwap.exe`를 실행합니다.
3. ARAM 챔피언 선택에서 벤치/공유풀이 생기면 overlay가 표시됩니다.
4. overlay의 챔피언 아이콘을 클릭하면 해당 챔피언으로 벤치 스왑을 요청합니다.

앱은 Windows 트레이에도 상주합니다. 트레이 메뉴에서 패널 표시 또는 종료를 할 수 있습니다.

## 빌드와 테스트

현재 프로젝트는 .NET Framework 4.0을 대상으로 하며 Windows의 Visual Studio/MSBuild로 빌드합니다.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe' .\tests\AramBenchSwap.Tests\AramBenchSwap.Tests.csproj /t:Rebuild /p:Configuration=Debug /v:minimal
.\tests\AramBenchSwap.Tests\bin\Debug\AramBenchSwap.Tests.exe

& 'C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe' .\src\AramBenchSwap.App\AramBenchSwap.App.csproj /t:Rebuild /p:Configuration=Release /v:minimal
```

Release 빌드 결과:

```powershell
.\src\AramBenchSwap.App\bin\Release\AramBenchSwap.exe
```

## Lockfile 탐색

앱은 LCU 접속 정보를 찾기 위해 다음 위치를 확인합니다.

- `LCU_LOCKFILE` 환경 변수
- 실행 중인 `LeagueClientUx` / `LeagueClient` 프로세스의 폴더
- `C:\Riot Games`, `C:\Program Files` 아래의 일반적인 설치 경로

## 알려진 제한

- 로컬 LCU endpoint에 의존하므로 Riot 클라이언트 업데이트로 동작이 깨질 수 있습니다.
- League Client 창이 화면 맨 위에 있으면 overlay가 화면 밖으로 나가지 않도록 화면 상단에 붙습니다.
- 첫 버전은 LCU 요청을 동기 방식으로 처리하므로, 로컬 클라이언트 응답이 느리면 overlay가 잠깐 멈출 수 있습니다.
- 픽창에서는 챔피언 이름을 표시하지 않습니다. 빠른 식별과 낮은 시각적 방해를 위해 아이콘만 보여줍니다.

## 라이선스

MIT

---

## English

Small Windows utility for quick manual ARAM bench swaps through the local League Client API (LCU).

## What It Does

- Watches the local League Client champ-select session.
- Shows a small always-on-top panel when an ARAM bench is available.
- Sends a bench swap request only when you directly click a champion icon.
- Does not auto-pick, auto-rank, auto-prioritize, or auto-swap champions.

The swap request is:

```http
POST /lol-champ-select/v1/session/bench/swap/{championId}
```

## Important Notice

This project is an unofficial personal utility. It is not endorsed by Riot Games and does not use an officially supported third-party API surface. LCU endpoints can change without notice.

Use it at your own risk and keep the behavior manual: the app is designed around direct user clicks, not automated champion selection.

## Download

For normal use, download the latest `AramBenchSwap-win-x64.zip` from GitHub Releases, extract it, and run `AramBenchSwap.exe`.

Required files in the zip:

```text
AramBenchSwap.exe
AramBenchSwap.Core.dll
```

## Usage

1. Start League of Legends.
2. Run `AramBenchSwap.exe`.
3. Enter an ARAM champ select where the bench/shared pool is available.
4. Click a champion icon in the overlay to request a bench swap.

The app also stays in the Windows tray. Use the tray menu to show the panel or exit the app.

## Build And Test

This project currently targets .NET Framework 4.0 and builds with Visual Studio/MSBuild on Windows.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe' .\tests\AramBenchSwap.Tests\AramBenchSwap.Tests.csproj /t:Rebuild /p:Configuration=Debug /v:minimal
.\tests\AramBenchSwap.Tests\bin\Debug\AramBenchSwap.Tests.exe

& 'C:\Program Files\Microsoft Visual Studio\2022\Community\Msbuild\Current\Bin\MSBuild.exe' .\src\AramBenchSwap.App\AramBenchSwap.App.csproj /t:Rebuild /p:Configuration=Release /v:minimal
```

Release output:

```powershell
.\src\AramBenchSwap.App\bin\Release\AramBenchSwap.exe
```

## Lockfile Lookup

The app tries these sources:

- `LCU_LOCKFILE` environment variable
- running `LeagueClientUx` / `LeagueClient` process directory
- common install paths under `C:\Riot Games` and `C:\Program Files`

## Known Limitations

- The app depends on local LCU endpoints and may break if Riot changes the client API.
- If the League Client is at the very top of the screen, the overlay clamps to the screen top instead of going off-screen.
- LCU requests are synchronous in this first version, so a slow local client response can briefly pause the overlay.
- Champion names are not shown in champ select; the overlay is intentionally icon-only for speed and low visual noise.

## License

MIT
