# Notepad By FerryPot

A modern, buttery-smooth Windows desktop text editor built with official **Google Material Design 3 (M3) Stock Design**, multi-tab workspace, and extensive customization options.

![Windows](https://img.shields.io/badge/Platform-Windows%20x64-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-purple)
![Design](https://img.shields.io/badge/Design-Google%20Material%203-emerald)

---

## 🚀 Quick Launch

You can run the application directly by double-clicking:
```
D:\Notepad By FerryPot\NotepadByFerryPot.exe
```

---

## ✨ Features

### 🎨 Material Design 3 (M3) Stock Aesthetics
- **Theme Modes**: System Default, Light Mode, and Dark Mode.
- **Dynamic Color Schemes**: Deep Purple (M3 Baseline), Google Blue, Emerald Green, Sunset Amber, Crimson Coral, Indigo, Cyan, Pink.
- **AMOLED Mode**: Pure Black (`#000000`) background option for OLED displays.
- **M3 Elevation & Components**: Rounded M3 chips, cards, floating action buttons, icon buttons, and navigation bar.

### 📑 Multi-Tab Workspace
- Open and manage multiple documents side-by-side.
- Clean tab chips with modification status indicator (`●`).
- Save prompt when closing modified tabs or exiting the application.
- Quick `+` button to create new documents instantly.

### ✍️ High-Performance Editor Engine
- **Synchronized Line Numbers**: Gutter with real-time smooth scrolling in tandem with text.
- **Word Wrap**: Toggle wrap on or off on the fly.
- **Smart Indentation**: Automatically copies leading tabs/spaces to subsequent lines on Enter.
- **Auto-Close Brackets & Quotes**: Auto-pairs `()`, `[]`, `{}`, `""`, and `''`.
- **Customizable Tab Indent**: Choose between 2, 4, or 8 spaces, or raw tab characters.
- **Zooming**: Smooth zoom in/out with `Ctrl + Plus`, `Ctrl + Minus`, or `Ctrl + 0`.

### 🔍 Find & Replace
- Fast modeless dialog with **Find Next**, **Replace**, and **Replace All**.
- Options for **Match Case**, **Match Whole Word**, and **Search Up / Down**.

### 📊 Document Statistics
- Real-time word and character counters in the status bar.
- Comprehensive statistics dialog: Words, Characters (with/without spaces), Lines, Estimated Reading Time, and Speaking Time.

### 📁 File & Shell Integration
- Drag-and-drop any text file (`.txt`, `.md`, `.cs`, `.json`, `.py`, `.js`, etc.) into the window to open it.
- Encoding support: `UTF-8`, `UTF-16 LE`, `ASCII`.
- Line ending normalization: `Windows (CRLF)` vs `Unix (LF)`.
- Standard Win32 Print integration.

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + N` | New Document Tab |
| `Ctrl + O` | Open File(s) |
| `Ctrl + S` | Save Current File |
| `Ctrl + Shift + S` | Save As |
| `Ctrl + W` | Close Active Tab |
| `Ctrl + F` / `Ctrl + H` | Find & Replace |
| `Ctrl + P` | Print Document |
| `Ctrl + ,` | Open Settings & Customization |
| `F11` | Toggle Fullscreen / Focus Mode |
| `F5` | Insert Current Date & Time |
| `Ctrl + +` / `Ctrl + -` | Zoom In / Zoom Out |
| `Ctrl + 0` | Reset Zoom to 100% |

---

## 🛠️ Rebuilding from Source

To build a fresh release single-file executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "D:\Notepad By FerryPot\Publish"
```
