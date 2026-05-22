# ARAM Bench Swap

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
