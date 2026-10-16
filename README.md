<!-- Copyright (C) 2026 SharpShooter contributors -->
<!-- SPDX-License-Identifier: GPL-2.0-or-later -->

# SharpShooter

**See the signal. Find the problem.**

[![Build](https://github.com/voidborn-astra/SharpShooter/actions/workflows/build.yml/badge.svg)](https://github.com/voidborn-astra/SharpShooter/actions/workflows/build.yml)

SharpShooter is a desktop log analyzer for the [SharpEmu](https://github.com/sharpemu/sharpemu)
PS5 emulator. Point it at a folder of SharpEmu logs and it shows each run's frame
rate, render costs, frame pacing, and errors, then lets you compare runs of the
same game side by side. It runs on Windows, Linux, and macOS, reads logs only,
and never uploads anything.

## Features

- **Game-aware run list.** Each log shows the game name, title ID, version, FPS,
  run length, issue count, and whether the emulator exited cleanly.
- **Overview.** Submitted and presented FPS, sample coverage, an FPS chart, and
  the largest reported render costs.
- **Repeated issues collapsed.** A warning that repeats 50,000 times is shown once
  with its count and first line number, so rare failures are not buried.
- **Compare runs.** Overlay FPS charts and a side-by-side table with the change
  from your baseline run. Add or remove runs, including runs from other games.
- **Labels.** Name runs ("baseline", "new shader cache", "run 2") so comparisons
  stay readable.
- **Offline HTML export.** One self-contained report file with no external assets.
- **Large logs.** 100 MB logs load in about a second each. Per-event trace dumps
  are counted instead of stored, so memory stays low.

## Download

Get the latest build for your platform from the
[Releases](https://github.com/voidborn-astra/SharpShooter/releases) page. Each app
is about 24 MB and needs no .NET install.

| Platform | File | Contains |
| --- | --- | --- |
| Windows (x64) | `SharpShooter-<version>-win-x64.zip` | `SharpShooter.exe` |
| macOS (Apple Silicon) | `SharpShooter-<version>-osx-arm64.zip` | `SharpShooter.app` |
| macOS (Intel) | `SharpShooter-<version>-osx-x64.zip` | `SharpShooter.app` |
| Linux (x64) | `SharpShooter-<version>-linux-x64.tar.gz` | `SharpShooter` and `install.sh` |

### Windows

Extract the zip and double-click `SharpShooter.exe`. The app is not code-signed,
so SmartScreen may warn the first time: choose *More info*, then *Run anyway*.

### macOS

Open the zip and drag `SharpShooter.app` to *Applications*. The app is not
notarized by Apple, so macOS blocks the first launch:

1. Double-click `SharpShooter.app`. macOS says it cannot verify the app. Choose *Done*.
2. Open *System Settings → Privacy & Security*, scroll down, and choose *Open Anyway*.

After that it opens with a normal double-click. If macOS says the app "is damaged",
run `xattr -dr com.apple.quarantine /Applications/SharpShooter.app` once.

### Linux

Extract the archive and run the installer from that folder:

```sh
./install.sh
```

SharpShooter then appears in your application menu with its icon, and `sharpshooter`
works from a terminal. The installer needs no root: it copies the app to
`~/.local/bin` and adds a menu entry under `~/.local/share`. Run
`./install.sh --uninstall` to remove it; your settings are kept. You can also run
`./SharpShooter` directly without installing.

## Quick start

1. **Pick your log folder.** Paste the path to your SharpEmu log folder, or use
   *Browse*. Folders you have scanned before are under the history button.
2. **Scan.** *Newest* sets how many recent logs to read (1–100). Turn on
   *Subfolders* if your logs are sorted into folders.
3. **Select a run** from the list on the left. Filter by game name, title ID,
   file name, or label.
4. **Choose a time window.** *Full run*, *First 180 seconds*, *Last 20 seconds*, or
   a *Custom window*. Use *Trim end* to drop idle time before shutdown.
5. **Read the sections** using the tabs: Overview, Sections, Frame trace, Metrics,
   Issues, Environment, and Compare game.
6. **Label your runs** so you can tell a baseline from a change.
7. **Compare.** *Compare game* starts with every loaded run of the same game.
   *Choose runs* adds or removes runs. If older runs of the game were not loaded,
   *Load more runs* reads them.
8. **Export** an offline HTML report to share.

The game name and version come from the `[LOADER] Title:` line that SharpEmu
writes from `param.json`. Logs without that line show the title ID instead.

## Sections

| Section | What it shows |
| --- | --- |
| Overview | Stat tiles, FPS chart, run details, and the largest render costs |
| Sections | The run in fixed 20-second intervals with FPS, coverage, and issues |
| Frame trace | Frame gap percentiles and per-phase costs from retained frame traces |
| Metrics | Every numeric report series, searchable, with latest values or cumulative deltas |
| Issues | Counts by category and repeated messages grouped with counts and line numbers |
| Environment | Environment variables found in the log, inferred features, config evidence |
| Compare game | Run picker, FPS overlay, and a side-by-side table against the oldest run |

## Command-line report

Create the HTML report without opening the window:

```sh
SharpShooter --report report.html /path/to/log1.txt /path/to/log2.txt
```

Use absolute paths. The exit code is 0 on success and 1 on failure.

## Privacy and data

- Logs are opened read only. SharpShooter does not launch games or change settings.
- Nothing is uploaded. HTML exports contain no external scripts, fonts, or images.
- Labels, recent folders, and scan options are saved locally:
  - Windows: `%LOCALAPPDATA%\SharpShooter\settings.json`
  - Linux: `~/.local/share/SharpShooter/settings.json`
  - macOS: `~/Library/Application Support/SharpShooter/settings.json`

## Reading the numbers

SharpShooter reports what the log shows and says when data is missing. It
identifies evidence, not proven causes.

- **FPS.** Averages are time-weighted between adjacent samples. The first sample
  and gaps over 10 seconds are excluded. Missing samples are not zero FPS, so
  coverage is always shown. *Submitted FPS* is the render rate; *presented FPS*
  can include repeated frames.
- **Render costs.** `RENDER_MS` values are milliseconds per frame, averaged by frame
  count. Nested costs can overlap, so they cannot be added into a frame total.
- **Frame traces** use their own timestamp clock and are not filtered by the time
  window. Gaps are present-loop gaps, not GPU-only time.
- **Numeric reports** keep their field names and units. Counts, bytes, and time must
  not be added together. Cumulative deltas need two observed endpoints.
- **Trace dumps** (lines with `sequence=` and `timestamp=`, such as `MUTEX_HANDOFF`)
  are counted, not stored. `[PERF][FRAME]` lines are still read as frame traces.
- **Environment** values are text found in the log, not a snapshot of the process.
  A missing variable is unknown, not disabled.
- **Issues** are grouped after numbers and addresses are masked. Counts are matching
  lines, not unique bugs. One pattern can cover unrelated calls.
- **Comparisons.** Equal time windows do not prove equal scenes. Match the route,
  settings, cache state, and run order before drawing conclusions.
- **Limits.** Issue details keep 5,000 lines, numeric reports 250,000, and frames
  100,000. Issue grouping keeps 2,000 patterns. The app reports when a limit is hit.

## Building from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet test SharpShooter.slnx -c Release
dotnet run --project SharpShooter.csproj -c Release
```

To publish a single executable, set the runtime ID (`win-x64`, `linux-x64`,
`osx-x64`, or `osx-arm64`):

```sh
dotnet publish SharpShooter.csproj -c Release -r linux-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=true \
  -p:DebugType=none -o publish
```

Published builds are trimmed, so code that relies on reflection fails the build.
Use the source-generated contexts in `Services/JsonContext.cs` for JSON.

Test fixtures are written to the system temp folder. Set `SHARPSHOOTER_TEST_OUTPUT`
to use another folder.

### CI/CD

[`.github/workflows/build.yml`](.github/workflows/build.yml) runs the tests on
Windows, Linux, and macOS for every push and pull request, then publishes all four
builds as workflow artifacts. The macOS jobs wrap the executable in
`SharpShooter.app` with [`packaging/macos/make-app.sh`](packaging/macos/make-app.sh)
(icon, `Info.plist`, and an ad-hoc signature, which Apple Silicon requires). The
Linux archive includes [`packaging/linux/install.sh`](packaging/linux/install.sh).
Pushing a version tag creates a release:

```sh
git tag v0.2.0
git push origin v0.2.0
```

The tag sets the version shown in the app. Other builds use the project version
with a CI suffix, such as `0.1.0-ci.42`. Dependabot keeps actions and packages current.

## Project layout

| Folder | Contents |
| --- | --- |
| `Analysis` | Log parser, models, and one analysis module per section |
| `Views` | The main window and one view per section |
| `Controls` | Shared controls: FPS chart, tables, value bars, stat tiles |
| `Services` | Settings, HTML export, JSON contexts, and file opening |
| `Themes` | Colors and styles, matched to the SharpEmu launcher |
| `Assets` | App icon and the Material Symbols Rounded icon font |
| `packaging` | macOS app bundle script and Linux menu installer |
| `tests` | Parser, analysis, view, safety, and file-size tests |

## Contributing

- Keep each file under 500 lines. A test enforces this.
- Keep parsing, analysis, views, controls, and services in separate focused files.
- Add an SPDX license header to every new file.
- Treat logs as untrusted input. Escape everything that goes into exports.
- Add a focused test for each new parser or analysis feature.
- Do not reference SharpEmu projects. SharpShooter reads SharpEmu output only.

## License

SharpShooter is licensed under [GPL-2.0-or-later](LICENSES/GPL-2.0-or-later.txt).
The Material Symbols Rounded font is © Google LLC under
[Apache-2.0](LICENSES/Apache-2.0.txt). The theme is adapted from the SharpEmu
launcher, which is also GPL-2.0-or-later.
