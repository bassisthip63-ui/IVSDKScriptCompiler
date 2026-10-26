# IVSDKScriptCompiler

**Run ScriptHookDotNet scripts straight from `.cs` / `.vb` source under [IV-SDK .NET](https://github.com/ClonkAndre/IV-SDK-DotNet), the way ScriptHookDotNet 1.7.1.9 did.**

Status: **experimental**, version 0.3. Tested with IV-SDK .NET 1.9.1 on GTA IV Complete Edition 1.2.0.59. Nothing in it depends on the game version.

ScriptHookDotNet 1.7.1.9 could load a script from its source file: drop `MyMod.cs` into the `scripts` folder and it ran. IV-SDK .NET's ScriptHookDotNet support only loads built `.net.dll` files, so mods that ship as source, such as Chop Shops' `ChopShop.cs`, do nothing. IVSDKScriptCompiler is a small IV-SDK .NET script that fills that gap. When the game starts, it compiles those source files into `.net.dll` files, and IV-SDK .NET loads them like any other ScriptHookDotNet script.

---

## Contents

- [Requirements](#requirements)
- [Install](#install)
- [Using it](#using-it)
- [Which files get compiled](#which-files-get-compiled)
- [References (using other libraries)](#references-using-other-libraries)
- [Errors and the log](#errors-and-the-log)
- [Limits](#limits)
- [What it does on your PC (antivirus notes)](#what-it-does-on-your-pc-antivirus-notes)
- [How it works](#how-it-works)
- [Uninstall](#uninstall)
- [Troubleshooting](#troubleshooting)
- [Changelog](#changelog)
- [License](#license)

---

## Requirements

- GTA IV or EFLC with **IV-SDK .NET 1.9.x** installed and its **ScriptHookDotNet support** enabled. `IVSDKDotNet\bin\ScriptHookDotNet.dll` must be there.
- **Windows with .NET Framework 4.x.** Every supported Windows version already has it. The build uses the C# compiler that ships with it (`%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`), so you don't need Visual Studio.

## Install

The repository holds source code only. You build the one file yourself, which takes a few seconds:

1. Download this repository (green **Code** button, then **Download ZIP**) and unzip it.
2. Put the `IVSDKScriptCompiler` folder **inside your GTA IV folder**, next to `GTAIV.exe`.
3. Double-click **`build.bat`**. It builds `IVSDKScriptCompiler.ivsdk.dll` against the IV-SDK .NET in *that* game folder, then copies it to `IVSDKDotNet\scripts\`. When it's done it shows:
   ```
   Built and installed: ...\IVSDKDotNet\scripts\IVSDKScriptCompiler.ivsdk.dll
   ```
4. Start the game.

Building it yourself means the tool always matches your IV-SDK .NET install, and you can read every line of what you're running.

### Quick test

Copy `examples\HelloSource.net.cs` into the game's **`scripts`** folder and start the game. A few seconds after you're in control, this shows on screen:

```
HelloSource.net.cs: compiled from source and running
```

The IV-SDK console (F4) shows `HelloSource.net.cs -> HelloSource.compiled.net.dll`. Delete the file again once you've seen it work.

## Using it

Put source scripts where you'd have put them for ScriptHookDotNet 1.7.1.9, then start the game:

```
GTA IV\
  scripts\
    ChopShop.cs                 <- you add this
    ChopShop.compiled.net.dll   <- IVSDKScriptCompiler makes this; IV-SDK .NET loads it
```

- **First start after adding or changing a script:** it gets compiled, and it loads on that same start.
- **Later starts:** nothing is compiled. The existing `.compiled.net.dll` is used, so startup stays fast. A script is only rebuilt when its source file is newer than its compiled file, or when IVSDKScriptCompiler itself was updated.
- **Removing a script:** delete the `.cs` / `.vb` file. On the next start, its `.compiled.net.dll` is deleted too.
- Don't edit or hand out the `.compiled.net.dll` files. They're rebuilt from the source whenever needed.

## Which files get compiled

| Folder | Files compiled | Why |
|---|---|---|
| `<GTA IV>\scripts\` | every `*.cs` and `*.vb` | Same as ScriptHookDotNet 1.7.1.9 |
| `<GTA IV>\IVSDKDotNet\scripts\` | only `*.net.cs` and `*.net.vb` | That folder also holds IV-SDK .NET's own files |

The output is always `<name>.compiled.net.dll` next to the source. `ChopShop.cs` and `ChopShop.net.cs` both become `ChopShop.compiled.net.dll`.

Compiler settings: .NET Framework 4 CodeDom compiler (`CompilerVersion v4.0`), `/platform:x86 /optimize+`, no debug info. Warnings are ignored.

## References (using other libraries)

Every script is compiled with these references:

`mscorlib`, `System`, `System.Core`, `System.Drawing`, `System.Windows.Forms`, `System.Xml`, `System.Data`, and IV-SDK .NET's `ScriptHookDotNet.dll`

To add more, put a line anywhere in the script:

```csharp
//#reference SomeLibrary.dll
```

```vb
'#reference SomeLibrary.dll
```

A bare file name is looked for, in order, in the script's folder, the game folder, `IVSDKDotNet\bin`, and the .NET Framework folder. A full path is used as given. Names the compiler already knows, such as `System.Web.dll`, also work.

## Errors and the log

If a script doesn't compile, it simply doesn't load. The game and your other scripts carry on. Errors (file, line, column, message; up to 20 per script) go to:

- the **IV-SDK .NET console** (F4)
- **`IVSDKDotNet\IVSDKScriptCompiler.log`**, rewritten on every start

Example:

```
19:41:07  IVSDKScriptCompiler 0.3: looking for source scripts (.cs / .vb)
19:41:08    ChopShop.cs -> ChopShop.compiled.net.dll
19:41:08  IVSDKScriptCompiler: 1 source script(s): 1 compiled, 0 up to date, 0 failed
19:41:08  IVSDKScriptCompiler: refreshed IV-SDK's script file list, so new scripts load now
```

IV-SDK .NET's own log also shows two **harmless** yellow lines for each source script. IV-SDK .NET looks at the source files too, and it can't load source code itself:

```
The file 'ChopShop.cs' is not a IV-SDK .NET or ScriptHookDotNet script file.
Not loading script 'HelloSource.net.cs' because it is already loaded.
```

## Limits

- **Language version:** the compilers built into .NET Framework 4 understand **C# 5 / VB 11**. Old ScriptHookDotNet scripts are fine. Newer C# syntax won't compile: `$"..."` strings, `?.`, `nameof`, `=>` members, `out var`, and so on.
- **ScriptHookDotNet compatibility** is down to IV-SDK .NET's ScriptHookDotNet port. A script that compiles but misbehaves would do the same as a ready-built `.net.dll`.
- **Changes need a restart.** Source scripts are compiled only when the game starts, so restart the game after adding or editing one.

## What it does on your PC (antivirus notes)

Full disclosure, so you know what to expect and can check it yourself:

- **You build it on your PC from this source.** There is no prebuilt executable, no installer, no packer and no obfuscation.
- At game start it **runs the C# / VB compiler that ships with Windows** (`csc.exe` / `vbc.exe` from .NET Framework 4). That's how .NET's built-in `CodeDomProvider` works: it starts the compiler as a short-lived child process of the game, writes temporary files to your `%TEMP%` folder, and writes the finished `.compiled.net.dll` next to the source script. This is the same thing ScriptHookDotNet 1.7.1.9 did.
- It **reads and writes files only** in `scripts\`, `IVSDKDotNet\scripts\` and its own log in `IVSDKDotNet\`.
- It **updates one list inside IV-SDK .NET**, its list of script files, using .NET reflection (explained [below](#how-it-works)). It doesn't touch the game's memory, other processes or the registry.
- **No network access**, no downloads, no data collection.

Some antivirus programs dislike any program that compiles code at runtime, or a new `.dll` appearing in a game folder. If yours complains:

1. Scan the files yourself. In Windows, right-click the file and choose **Scan with Microsoft Defender**. You can also upload the built `IVSDKScriptCompiler.ivsdk.dll` to [VirusTotal](https://www.virustotal.com).
2. If Defender flags it wrongly, report the false positive to Microsoft at <https://www.microsoft.com/en-us/wdsi/filesubmission>.
3. Please [open an issue](https://github.com/bassisthip63-ui/IVSDKScriptCompiler/issues) here too, saying which antivirus program, its version and the detection name.

## How it works

1. IV-SDK .NET constructs its own `.ivsdk.dll` scripts **before** it loads ScriptHookDotNet scripts. IVSDKScriptCompiler does all its work in its constructor, so the compiled `.net.dll` files exist by the time IV-SDK .NET looks for ScriptHookDotNet scripts.
2. IV-SDK .NET loads scripts from memory, so `Assembly.Location` is empty. The tool finds its own file as `IVSDKDotNet\scripts\IVSDKScriptCompiler.ivsdk.dll` to know when it was itself updated.
3. **The file list.** IV-SDK .NET doesn't look at the folders when it loads ScriptHookDotNet scripts. It uses a list (`Manager.Managers.FileCacheManager.cachedFiles`) made when the game starts and kept up to date by a background file watcher. A `.dll` written a moment earlier usually isn't on that list yet, so a new script would only load on the *next* start. After compiling or removing anything, IVSDKScriptCompiler refreshes the two entries for `scripts\` and `IVSDKDotNet\scripts\`, the same way IV-SDK .NET's own watcher does. If a future IV-SDK .NET version changes that list, the tool logs it and skips this step, and new scripts just load on the next start.

The whole tool is the one file `IVSDKScriptCompiler.cs`, about 260 lines.

## Uninstall

Delete `IVSDKDotNet\scripts\IVSDKScriptCompiler.ivsdk.dll`, plus any `*.compiled.net.dll` files it made, and `IVSDKDotNet\IVSDKScriptCompiler.log`.

## Troubleshooting

| You see | Meaning / fix |
|---|---|
| `build.bat`: *Could not find IVSDKDotNetWrapper.dll* | The folder isn't inside the GTA IV folder. Put it next to `GTAIV.exe`. |
| `build.bat`: *Could not find the .NET Framework 4 C# compiler* | .NET Framework 4.x is missing or damaged. Install or repair it from Microsoft. |
| Log: *ScriptHookDotNet.dll not found - nothing compiled* | IV-SDK .NET's ScriptHookDotNet support isn't installed (`IVSDKDotNet\bin\ScriptHookDotNet.dll`). |
| Log: *NOT compiled (N error(s))* | The script has an error, or uses C# 6+ syntax. The lines above it name the file, line and problem. |
| Log: *error CS0246 ... could not be found* | The script needs another library. Add `//#reference ThatLibrary.dll`. |
| Log: *could not refresh / could not find IV-SDK's file list* | Your IV-SDK .NET version keeps the list differently. New scripts load on the next start instead. Please open an issue with your IV-SDK .NET version. |
| Log: *can't replace X.compiled.net.dll* | The file is in use. Restart the game and it is recompiled. |

## Changelog

See [CHANGELOG.md](CHANGELOG.md).

## License

MIT. See [LICENSE](LICENSE). IV-SDK .NET is by [ItsClonkAndre](https://github.com/ClonkAndre); this project isn't part of it and isn't endorsed by its author. GTA IV is a trademark of Rockstar Games / Take-Two Interactive; this project isn't affiliated with them.

Written by bassisthip63-ui with Claude (Anthropic).
