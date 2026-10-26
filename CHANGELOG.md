# Changelog

## 0.3 (2026-10-25)
- A brand-new or changed script now loads on the **same** start it is compiled. IV-SDK .NET loads ScriptHookDotNet
  scripts from a file list made at game start and updated by a background file watcher, which was too late for a
  `.dll` written a moment earlier. The tool now refreshes that list itself after compiling or removing anything
  (log line: *refreshed IV-SDK's script file list*). If the list isn't found, it logs that and new scripts load on
  the next start, as in 0.2.
- Version information (product, company, copyright, file version) added to the built `.dll`.

## 0.2
- Fixed *"The path is not of a legal form"* at start: IV-SDK .NET loads scripts from memory, so the tool's own
  location is empty; it now finds its file by name.
- Plain `*.cs` / `*.vb` files in the game's `scripts` folder are compiled too, like ScriptHookDotNet 1.7.1.9.

## 0.1
- First version: compiles `*.net.cs` / `*.net.vb` into `.compiled.net.dll` files at game start.
