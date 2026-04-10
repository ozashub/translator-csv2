# translator-csv2

A tiny WPF app that translates whatever text you have selected and drops the result in a small overlay next to your cursor. Select some foreign text anywhere on screen, press your hotkey, and a translation appears where you can actually read it without tabbing out.

This is a rewrite of translator-cs and translator-rs, both of which worked but had their own problems. The old C# build was on WinUI 3 and was fighting the framework for features that should have been easy; the Rust/Tauri one ran two overlay systems in parallel. This one uses plain WPF, which has been doing transparent topmost windows since Vista and is genuinely the right tool for the job.

## How it works

You highlight text in any app, press the hotkey, and the app simulates Ctrl+C to grab your selection. That text goes straight to OpenAI with a translate prompt built from your source and target language. The result pops up in a transparent window positioned just off your cursor, on whichever monitor you are working on, with the DPI of that monitor taken into account so nothing ends up microscopic on a 4K display.

Click the overlay to dismiss it, or leave it alone and it hides itself after twelve seconds. The clipboard gets restored to whatever you had in it beforehand, so the translate hotkey does not trample on your actual work.

## Features

- One global hotkey, captured with a low level keyboard hook so it works regardless of which window has focus
- Settings window with a dropdown for source language (or Auto) and target language
- System tray icon with a settings shortcut and a quit button
- API key stored in `%APPDATA%\TranslatorCsV2\config.json` and encrypted with DPAPI tied to the current Windows user
- Per monitor DPI aware, so the overlay positions correctly on mixed DPI setups
- Clipboard save and restore around the capture, so your clipboard history is untouched
- Click to dismiss, auto hide after twelve seconds

## Build

You need the .NET 8 SDK with the Windows Desktop workload installed.

```
dotnet build
dotnet run
```

The first launch will pop the settings window because there is no API key or hotkey set. Paste your OpenAI key in, click the hotkey box and press the combo you want, pick your languages, save. You are done.

## Layout

```
App.xaml(.cs)              app entry, single instance mutex
src/
  Translator.cs            top level orchestrator, wires everything
  config/
    Config.cs              config record
    ConfigStore.cs         json load/save with DPAPI on the api key
  input/
    KeyCombo.cs            combo record with scan code matching
    GlobalHotkey.cs        WH_KEYBOARD_LL hook on a dedicated thread
  clipboard/
    Selection.cs           SendInput Ctrl+C plus clipboard save and restore
  ai/
    Languages.cs           language list and prompt builder
    OpenAiClient.cs        chat completions over raw HttpClient
  overlay/
    OverlayWindow.xaml(.cs)  transparent topmost popup
    Cursor.cs              GetCursorPos, monitor DPI, edge flip
  tray/
    TrayIcon.cs            NotifyIcon wrapper
  ui/
    SettingsWindow.xaml(.cs)  dark themed config UI
```

## Notes

The hotkey uses scan codes rather than virtual keys so your combo stays the same even if you switch keyboard layouts mid session. The clipboard wait uses `GetClipboardSequenceNumber` rather than a fixed sleep, which was the main source of flakiness in translator-cs on slow machines. The overlay is positioned with raw `SetWindowPos` in physical pixels because WPF's per monitor DPI coordinate mixing is miserable to reason about and this sidesteps it entirely.

Default model is `gpt-4o-mini`. You can edit it in the json if you want something else.
