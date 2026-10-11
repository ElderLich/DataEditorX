# DataEditorX
DataEditorX is a card database and Lua script editor for ygopro, MDPro3, and custom card projects.

## Downloads
* Windows 32-bit: https://github.com/ElderLich/DataEditorX/releases/download/MDPro3/DataEditorX_win32.zip
* Windows 64-bit: https://github.com/ElderLich/DataEditorX/releases/download/MDPro3/DataEditorX_win64.zip

## Requirements
Install the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) if Windows cannot start the app.

## Features
* Create and edit `.cdb`, `.db`, and `.lua` files.
* Compare, copy, paste, add, modify, and delete card records across databases.
* Edit Lua scripts with syntax highlighting, autocomplete, constants, function hints, card names, setnames, and counter tooltips.
* Switch between light, dark, and custom theme profiles from `View -> Theme`; theme choices are saved.
* Manage MDPro3 custom card projects from `Workspace -> Project Manager`.
* Add custom archetypes and counters through the editor menu, with duplicate ID checks against bundled and project `strings.conf` files.
* Check for updates in-app and install downloaded release zips without manually replacing files.

## Project Manager
Open `Workspace -> Project Manager` to configure:
* `MDPro3 Directory`
* `MDPro3 Data Directory`
* `Custom Project Directory`
* `Voice Pack Directory`

Directory fields keep the latest five paths for faster switching between projects.

The Custom Project tab can install, uninstall, restart MDPro3, open the current custom `.cdb`, and enable active sync. The resolved path list shows where card databases, scripts, pictures and overframe images will be read or written.

The integrated Project Manager was co-created with BullyWiiPlaza, bringing the custom-card manager workflow directly into DataEditorX.

## Archetypes and Counters
Use `Add Archetype` or `Add Counter` from the DataEditor menu bar.

Enter a hexadecimal ID, for example `fae` or `0xfae`, then enter the display name. DataEditorX checks bundled `data/lua/strings.conf`, the custom project `Expansions/strings.conf`, and the MDPro3 `Expansions/strings.conf` before saving.

New entries are written to the custom project:

```text
<Custom Project>/Expansions/strings.conf
```

If an MDPro3 directory is configured, that `strings.conf` is synced to:

```text
<MDPro3>/Expansions/strings.conf
```

## Themes
Open `View -> Theme` to choose a built-in profile or open the theme editor. The theme applies to the main window, dock panel, database editor, script editor, autocomplete popup, card list, Project Manager, and dialogs.

## Version Info and Updates
Open `Help -> Version Info` to see the app version, maintainer, original author, source fork, repository, runtime, and process architecture.

Use `Help -> Check for Updates` to read the GitHub version metadata. If a newer version is available, DataEditorX downloads the matching win32 or win64 release zip, offers to install it, extracts the update, and restarts.

## Data Folder Layout
Bundled data files are grouped by purpose:
* `data/languages`: UI language files and card info definitions.
* `data/lua`: Lua constants, strings, script templates, and function autocomplete data.
* `data/editor`: syntax highlighting resources.
* `data/assets`: shared bundled images.

## Language
Open `Help -> Language` to choose a UI language, then restart the application.

To add a language named `xxx`, add both files:
* `data/languages/language_xxx.txt` for the interface.
* `data/languages/cardinfo_xxx.txt` for card information.

Each line in `language_english.txt` and `cardinfo_english.txt` is separated by a tab. Translate the content on the right side of the tab, then put the translated files in `data/languages`.

## Credits
* Original author: [purerosefallen/DataEditorX](https://github.com/purerosefallen/DataEditorX)
* Based on the [Lyris12 fork](https://github.com/Lyris12/DataEditorX).
* Project Manager co-created with BullyWiiPlaza.
* Maintained for MDPro3 custom card workflows by [ElderLich](https://github.com/ElderLich/DataEditorX).

## Rush Duel cards

Choose **Rush** in the top Rule selector to search or edit MDPro3 Rush cards. The selector follows MDPro3's card ID range (`120000000`–`129999999`). Source Rush databases use `ot = 4`; existing OT values are preserved and newly created Rush cards use 4. The editor does not write the client-only `0x100` pool filter into official card records.

Use **Legend** (`0x8`) and **Maximum** (`0x8000`) in Card Types. Rush races, including Cyborg, round-trip without signed integer truncation. Preserve the `RD/...` pack line and any `MAXIMUM ATK = ...` line at the beginning of the description; MDPro3 reads those display details from the text. Maximum summoning behavior still belongs in the Lua script.

Rush Duel uses the top **Rule** selector. There is no separate Rush checkbox; existing Omega metadata is preserved when saving cards.

Run the editor regressions with `dotnet run --project tests/DataEditorX.RegressionTests.csproj` on Windows. They use temporary databases and do not edit the game's data.

Rush autocomplete is optional and separate from the standard API: place `_rush_function_english.txt` (English), `_rush_functions.txt` (Chinese) and `rush_constant.lua` in `data/lua`. The UI language chooses the function documentation, with fallback to the other file. These are editor reference files; game scripts still load the Rush helper library themselves.

## Theme presets

Choose a palette from **View > Theme**: **Midnight** (deep navy/cyan), **Nord** (slate/ice), **Plum** (violet/lavender), **Forest** (pine/mint), **Sandstone** (warm ivory), or **Paper** (neutral light/blue). The existing Light, Dark, Alice Light and Custom choices remain available. Presets apply to card editing, list rows, menus, inputs and the code editor.

Use **View > Theme > Customize...** to preview presets beside sample card rows and highlighted Lua. Changing a color creates a Custom palette. **OK** saves the choice; **Cancel** keeps your current theme. The dialog can be resized to show more color controls.
