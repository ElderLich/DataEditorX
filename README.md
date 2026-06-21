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
* Sync special-frame card IDs such as Egyptian Gods, Sacred Beasts, and Dark Synchro cards to MDPro3 `SpecialCards.json`.
* Check for updates in-app and install downloaded release zips without manually replacing files.

## Project Manager
Open `Workspace -> Project Manager` to configure:
* `MDPro3 Directory`
* `MDPro3 Data Directory`
* `Custom Project Directory`
* `Voice Pack Directory`

Directory fields keep the latest five paths for faster switching between projects.

The Custom Project tab can install, uninstall, restart MDPro3, open the current custom `.cdb`, and enable active sync. The resolved path list shows where card databases, scripts, pictures, overframe images, and `SpecialCards.json` will be read or written.

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
* Maintained for MDPro3 custom card workflows by [ElderLich](https://github.com/ElderLich/DataEditorX).
