# The Long Dark Save Editor

Unofficial desktop save editor for **The Long Dark**.

This project is a WPF/.NET 8 application for inspecting and editing local save data for Wintermute and Survival saves, with inventory tools, player stat editing, affliction cleanup, map support, profile data editing, and raw JSON access for advanced users.

## Features

- Detects local save files automatically
- Supports Wintermute and Survival single-file saves
- Edits player vitals, condition, calories, thirst, fatigue, freezing, and carry-related values
- Manages inventory items, bulk add/remove, duplication, condition edits, and category filtering
- Edits skills, profile traits, feats, and afflictions
- Shows local bundled maps inside the app instead of opening a browser
- Creates backups before writing save changes
- Includes raw JSON editors for advanced save inspection

## Tech Stack

- C#
- .NET 8
- WPF
- Newtonsoft.Json

## Project Layout

- `CodexTldSaveEditor.App/` - WPF application source
- `CodexTldSaveEditor.App/tools/` - helper scripts for publishing and asset generation
- `release/TheLongDarkSaveEditor/` - local publish output used for running the app on this machine
- `OPEN-THE-LONG-DARK-SAVE-EDITOR.cmd` - local launcher for the published build

## Build

```powershell
dotnet build .\CodexTldSaveEditor.sln -c Release
```

## Publish Local Release

Use the included script to rebuild the distributable app into `release/TheLongDarkSaveEditor`:

```powershell
powershell -ExecutionPolicy Bypass -File .\CodexTldSaveEditor.App\tools\PublishRelease.ps1
```

## Run

After publishing, start the packaged app:

```powershell
.\OPEN-THE-LONG-DARK-SAVE-EDITOR.cmd
```

## Notes

- This is an unofficial fan tool and is not affiliated with Hinterland Studio.
- Save files are backed up before edits are written.
- Bundled wiki map images were sourced from The Long Dark Wiki. Attribution is included in `CodexTldSaveEditor.App/Assets/Maps/Wiki/ATTRIBUTION.txt`.
- Licensed under the MIT License. See `LICENSE` for details.

## Recommended GitHub Release Workflow

1. Run `PublishRelease.ps1`
2. Test the app from `release/TheLongDarkSaveEditor`
3. Commit source changes
4. Push the repository to GitHub
5. Create a GitHub Release and attach the published files as a zip if you want to distribute binaries

