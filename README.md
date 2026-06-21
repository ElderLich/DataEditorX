# DataEditorX
DataEditorX is a card database and Lua script editor for [ygopro](https://github.com/Fluorohydride/ygopro), MDPro3, and related custom card workflows.

## Downloads
* Windows 32-bit: https://github.com/ElderLich/DataEditorX/releases/download/MDPro3/DataEditorX_win32.zip
* Windows 64-bit: https://github.com/ElderLich/DataEditorX/releases/download/MDPro3/DataEditorX_win64.zip

> **FAQ**   
Q: I can't run the program.   
A: Please install the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).

## Features
* Create and edit card databases.
* Compare, copy, and paste card records across databases.
* Read card records from ygopro deck files(.ydk) or card picture folders.
* Create and edit card scripts(.lua).
* Use autocomplete and function/card tooltips while editing Lua scripts.
* Export and import [MSE](https://github.com/247321453/MagicSetEditor2) sets.
* Open the project source from Help-->Source Code.
* Switch between light and dark theme from View-->Dark Theme.

## Dark Theme
Open View-->Dark Theme to toggle the app theme. The setting is saved in the app config, so the next launch keeps your preference.

The dark theme updates the main window, dock area, database editor, script editor, autocomplete popup, card list, and editor controls. It can be toggled while editor tabs are open.

## Version Info and Updates
Open Help-->Version Info to see the current DataEditorX version, maintainer, original author, repository, runtime, and process architecture.

Use Help-->Check for Updates to check the update metadata in `DataEditorX/readme.txt` on GitHub. If a newer version is available, DataEditorX downloads the matching win32 or win64 zip, offers to install it, closes the app, extracts the update, and restarts.

## Common Workflows
* Open `.cdb`, `.db`, or `.lua` files with File-->Open, or drag files/folders into the app.
* Create a new database or script with File-->New.
* Use the DataEditor tab to search, compare, copy, paste, modify, and export cards.
* Use the CodeEditor tab to edit scripts, check syntax, find/replace text, and connect card names from a selected database.
* Use Help-->Language to choose a UI language, then restart the application.

## Add a New Archetype
First decide the setcode, as a hex number, for the new archetype. Avoid conflicts with existing setcodes.

Type the setcode in the text box to the right of the archetype combo box and click Modify. To show the archetype name in the combo box, open `data/cardinfo_xxx.txt`, where `xxx` is the language, then add a new line between `##setname` and `#end`. Write the setcode starting with `0x`, then a tab, then the archetype name.

## Language
Open Help-->Language to choose a language, then restart the application.

To add a language named `xxx`, add both files:
* `data/language_xxx.txt` for the graphic interface.
* `data/cardinfo_xxx.txt` for card information.

Each line in `language_english.txt` and `cardinfo_english.txt` is separated by a tab. Translate the content on the right side of the tab, then put the translated files in `language_xxx.txt` and `cardinfo_xxx.txt`.

## Fork Additions
* Persistent dark theme toggle.
* Updated project links for https://github.com/ElderLich/DataEditorX.
* Source Code and external Lua file launches use the Windows shell, avoiding URL/file launch exceptions.
* SQLitePCLRaw package updates remove the known vulnerable package warning.
* In-app updater can install downloaded releases and restart DataEditorX.

## Release Script
Use `tools/release.py` to prepare release builds and keep version metadata in sync.

Build both zip assets for the `MDPro3` release tag:

```powershell
python tools/release.py --version 1.0.0
```

Build and upload both assets to GitHub:

```powershell
python tools/release.py --version 1.0.0 --upload
```

Upload uses the GitHub CLI, so run `gh auth login` first. The script creates or updates these assets:
* `DataEditorX_win32.zip`
* `DataEditorX_win64.zip`

## Special Features of KoishiDEX
1. The format of scripts will be in Koishi-Style when creating new scripts. Also the module script to be required will be adjustable, and will be packed when exporting zip files.
2. Scripts of Non-Pendulum Normal monsters will be openable, for creating module scripts.
3. Will ignore the card alias when opening a script.
