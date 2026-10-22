# Executive plan

The record of deliberate decisions in this repository: what was chosen, why, and how it was built and tested. Read it before changing architecture, data flow, dependencies or tests.

- Don't reverse an active decision without agreement. Record a new decision that supersedes it instead.
- Add or update an entry whenever functionality is added or changed.

## About this repository

- **Purpose:** A Windows screensaver (`Spirograph.scr`) that draws rounds of 3–5 randomised spirographs at random positions, holds them, fades out and starts again.
- **Stack:** C# on .NET Framework 4.8, Windows Forms and GDI+ (`System.Drawing`), built with the .NET SDK using an SDK-style project (`Spirograph.csproj`).
- **Run:** Windows only. Run `bin/Release/net48/Spirograph.scr /s` for full screen, or install it from the right-click menu (see `README.md`).
- **Test:** No automated tests.
- **Build:** `dotnet build -c Release`, which outputs `bin/Release/net48/Spirograph.scr`. GitHub Actions (`.github/workflows/build.yml`) builds every push to `main` and publishes a release for each `v*` tag.

## Standing decisions

| ID | Decision | Status | Entry |
| --- | --- | --- | --- |
| D-001 | Native C# `.scr`, not a web page in a host app or Electron | Active | 2026-10-22 · Initial build |
| D-002 | Target .NET Framework 4.8 so the file needs no runtime install | Active | 2026-10-22 · Initial build |
| D-003 | Build and release on GitHub Actions; a `v*` tag publishes the `.scr` | Active | 2026-10-22 · Initial build |
| D-004 | Near-black background `#0e0e10`, not pure black | Active | 2026-10-22 · Initial build |
| D-005 | Each round draws 3–5 patterns at spread-out random positions, then holds and fades | Active | 2026-10-22 · Initial build |
| D-006 | Fade is drawn as an overlay at paint time; the canvas isn't dimmed | Active | 2026-10-22 · Initial build |
| D-007 | No settings: random behaviour, `/c` shows an information message | Active | 2026-10-22 · Initial build |
| D-008 | Leave the workflow's actions on their Node.js 20 versions | Active | 2026-10-22 · Initial build |

## Entries

Newest first.

### 2026-10-22 · Initial build

**Status:** Done. Released as v1.0.0 and confirmed working on the owner's Windows PC.

**Goal:** A screensaver for the owner's Windows PC, also shareable with friends, that draws spirograph after spirograph using the gradients from the Spirograph web app in `~/GithubRepos/spirograph` (moved out of `front-end-ai-demos` on 2026-10-22).

**Context:** Developed on a Mac with no Windows machine available during the session. The target monitor is an AOC G2590FX, a 24.5" 1080p TN LCD panel. The owner asked for random placement rather than centred patterns, and for 3–5 patterns per screen before everything fades.

#### Decisions

- **D-001: Native C# screensaver.** Gives one small file with the standard Windows install (right-click → Install) and no third-party host. Rejected: running the web app as a screensaver through Lively Wallpaper, which depends on a third-party app and uses a browser engine's memory; Electron packaged as a `.scr`, which is about 100MB and awkward to run full screen. Constrains: drawing logic is a C# port of the web app, so the two don't share code.
- **D-002: .NET Framework 4.8.** Windows 10 and 11 include it, so the `.scr` is about 16KB and runs with nothing to install. Rejected: self-contained .NET 8, which is a 50MB+ single file; framework-dependent .NET 8, which requires friends to install the .NET Desktop Runtime. Constrains: no .NET 8-only APIs; Windows Forms and System.Drawing are referenced with `<Reference>`, not `UseWindowsForms`; `LangVersion` is `latest`, but language features that need newer runtime types (records, `init`) need shims, so avoid them.
- **D-003: GitHub Actions builds the releases.** The development machine is a Mac without a system-wide .NET SDK, and releases give friends a download link. Rejected: building locally and uploading by hand. Constrains: to ship a change, push a tag such as `v1.0.1`; a push to `main` only builds and uploads a workflow artifact.
- **D-004: Near-black background, `#0e0e10`.** The target monitor is an LCD, where static images don't cause burn-in, and the owner asked for dark grey or near-black. Rejected: pure black, which only matters for OLED screens. Constrains: if someone uses it on an OLED screen, pure black is the safer choice; the colour is `Scene.Background` in `src/Scene.cs`.
- **D-005: 3–5 spread-out patterns per round.** Requested by the owner. Each pattern's position is the best of 16 random candidates, picked to overlap earlier patterns least; overlap is still allowed when the screen is full. Radius is 16–32% of the screen's shorter side. Rejected: centring every pattern, which was the original idea.
- **D-006: Fade as a paint-time overlay.** During the fade, `OnPaint` draws the canvas and then a background-coloured rectangle whose opacity rises from 0 to 1, and the canvas is cleared when the fade ends. This is exact and cheap. Rejected: repeatedly drawing translucent rectangles onto the canvas, which builds up unevenly and never quite reaches the background colour without a final clear.
- **D-007: No settings window.** Every pattern is fully random, and `/c` (Settings in the Windows dialog) shows a short information message. Kept simple for the first version. On 2026-10-22 the owner confirmed they don't want a settings window, so don't add one unless they ask.
- **D-008: Workflow actions stay on their Node.js 20 versions.** `actions/checkout`, `actions/setup-dotnet` and `actions/upload-artifact` are at v4, which GitHub has deprecated because they run on Node.js 20. The workflow still runs, and the owner doesn't expect another release, so updating them isn't worth the change. Rejected: moving to newer major versions now. Constrains: before publishing a new release, check the workflow still runs; if GitHub has removed Node.js 20 support, update the actions first.

#### Implementation

- `src/Program.cs` reads the flag Windows passes: `/s` opens one window per monitor, `/p <hwnd>` runs the small preview, and anything else shows the information message. Flags can arrive as `/p 1234` or `/p:1234`, in either case.
- `src/ScreensaverForm.cs` is the borderless, top-most window. It runs a ~60fps timer and invalidates only the area the latest stroke touched while drawing, and the whole window during the fade.
- `src/Scene.cs` places, draws, holds and fades the patterns. Timing constants are at the top: pen speed 700 px/s at 1080p (scaled to the screen's height), 0.8s pause between patterns, 5s hold, 2.5s fade.
- `src/Spiro.cs` holds the spirograph maths, the gradients and the random settings.
- Gotchas:
  - The gradients and random ranges (ring 40–150 teeth, wheel 8 to ring−8 inside or up to 90 outside, 3–30 laps, pen hole 30–110%) are copied from the web app's `src/lib/spiro.ts`. They don't update automatically; keep them in step by hand.
  - `Spirograph.csproj` copies the built `.exe` to `.scr` after each build. Windows only treats the file as a screensaver because of that extension.
  - The process opts into per-monitor DPI awareness (`SetProcessDpiAwarenessContext`, falling back to `SetProcessDPIAware`). Without it, Windows scales the window and the lines blur on high-DPI screens.
  - The first `MouseMove` event fires when the window appears, so exit-on-mouse uses a 10px tolerance from the first position. Removing the tolerance makes the screensaver close immediately.
  - The preview window is a child of the Settings dialog (`SetParent` plus `WS_CHILD`). It closes itself when the parent window no longer exists.
  - Each pattern is drawn in short chunks, and each chunk takes the next gradient colour. A pattern is capped at 240,000 steps.

#### Testing

- Automated: none. `dotnet build -c Release` is the only check; it ran on macOS and runs in GitHub Actions on every push to `main` and every tag.
- Manual: on 2026-10-22 the owner installed it on their Windows PC and it ran correctly from the preview in Screen Saver Settings. The owner confirmed the drawing speed, hold and fade timing feel right. Later the same day, Windows started it on its own after the wait time, and it ran correctly on both of the owner's monitors.
- Not covered: high-DPI scaling hasn't been checked.

#### Open questions

- None.

#### Session log

- 2026-10-22: Built the screensaver, created the repo `alemulli/spirograph-screensaver` (now public so it can be shared), and published release v1.0.0 from GitHub Actions.
- 2026-10-22: The owner installed it on Windows. The preview in Screen Saver Settings works, and the timing is right. No settings window is wanted (D-007 confirmed).
- 2026-10-22: Windows started it on its own after the wait time and it ran correctly. Marked Done.
- 2026-10-22: Confirmed on a second monitor. Recorded D-008: no further releases are expected, so the workflow's actions stay as they are.
