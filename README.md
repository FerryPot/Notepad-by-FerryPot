# Notepad by FerryPot

A tabbed Windows text and Markdown editor inspired by Windows 11 Notepad, built with WPF, .NET 8, and Google Material Design 3.

[![Build](https://github.com/FerryPot/Notepad-by-FerryPot/actions/workflows/build.yml/badge.svg)](https://github.com/FerryPot/Notepad-by-FerryPot/actions/workflows/build.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![WPF](https://img.shields.io/badge/Framework-WPF-blue)](https://github.com/dotnet/wpf)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

<!-- Add screenshot: docs/screenshot.png -->
*(Screenshots can be found and contributed in the [docs/](docs/) folder: Formatted view, Markdown syntax view, and dark/AMOLED theme).*

---

## ✨ Features

- **Editing Engine**: Synchronized line numbers gutter, word wrap toggle, auto-indentation, auto-closing brackets and quotes (`()`, `[]`, `{}`, `""`, `''`), customizable tab sizes (2, 4, 8 spaces, or tabs).
- **Dual Markdown Modes**: 
  - *Formatted View*: Rich rendered preview with live editing of headings, bold, italic, strikethrough, lists, links, and tables.
  - *Markdown View*: Fast monospaced raw markdown editing with line gutter and syntax highlighting.
  - *Formatting Toolbar*: Dynamic active-state highlighting for Bold, Italic, Strikethrough, Headings (H1–H6/Normal), and Lists with pending style toggles.
- **Tabs & File Handling**: Multi-tab workspace with horizontal scroll overflow, dirty indicators (`●`), tab rename, drag-and-drop file opening, encoding support (UTF-8, UTF-16 LE, ASCII), line endings (CRLF, LF).
- **Appearance & Themes**: Official Google Material Design 3 styling with Amber accent, Light, Dark, and AMOLED (pure black `#000000`) modes, customizable system fonts, and smooth zoom (10% to 500%).
- **Productivity Tools**: Modeless Find & Replace with case matching and whole-word filters, Document Statistics (words, characters, lines, reading/speaking time), and distraction-free Focus/Zen mode (<kbd>F11</kbd>).

---

## 📥 Download & Install

### Running the App
1. Download the latest release from the [Releases](https://github.com/FerryPot/Notepad-by-FerryPot/releases) page.
2. Extract the ZIP archive or open the downloaded application to run immediately—no installer required.

> **Prerequisite:** This build relies on the [.NET 8 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0). If prompted, download and install the desktop runtime.
>
> **Windows SmartScreen Note:** Because this application is an open-source, community-compiled executable without an expensive commercial code-signing certificate, Windows SmartScreen may show a warning on first launch. Click **More info** and then **Run anyway**.

---

## 🛠️ Build from Source

### Prerequisites
- Windows 10 (version 1903+) or Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with *.NET desktop development* workload) or VS Code

### Build Commands

```powershell
# 1. Clone repository
git clone https://github.com/FerryPot/NotepadByFerryPot.git
cd NotepadByFerryPot

# 2. Restore dependencies
dotnet restore

# 3. Run in development mode
dotnet run

# 4. Compile in Release mode
dotnet build -c Release

# 5. Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "Publish"
```

---

## 📁 Project Structure

```text
├── Assets/                 # App icons (.ico, .png) and static assets
├── Controls/               # Custom controls (NotepadEditor dual-view text engine)
├── Models/                 # Data contracts (NotepadDocument document state)
├── Services/               # MarkdownParserService, ThemeService, SettingsService
├── Views/                  # Dialogs (FindReplaceWindow, SettingsWindow, StatsDialog)
├── App.xaml / App.xaml.cs  # Application entry point, themes, and global resources
├── MainWindow.xaml (.cs)   # Main application window (tabs, toolbar, status bar)
├── NotepadByFerryPot.csproj# Project configuration, WPF dependencies, and M3 packages
├── NotepadByFerryPot.exe   # Compiled ready-to-run release executable
└── README.md               # Project documentation
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action | Shortcut | Action |
| :--- | :--- | :--- | :--- |
| <kbd>Ctrl</kbd> + <kbd>N</kbd> | New Document Tab | <kbd>Ctrl</kbd> + <kbd>B</kbd> | Toggle Bold |
| <kbd>Ctrl</kbd> + <kbd>O</kbd> | Open File | <kbd>Ctrl</kbd> + <kbd>I</kbd> | Toggle Italic |
| <kbd>Ctrl</kbd> + <kbd>S</kbd> | Save File | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Toggle Strikethrough |
| <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>S</kbd> | Save As | <kbd>Ctrl</kbd> + <kbd>K</kbd> | Insert Link |
| <kbd>Ctrl</kbd> + <kbd>W</kbd> | Close Active Tab | <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>M</kbd> | Toggle Syntax / Formatted View |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> / <kbd>H</kbd> | Find & Replace | <kbd>F11</kbd> | Toggle Focus / Zen Mode |
| <kbd>Ctrl</kbd> + <kbd>P</kbd> | Print Document | <kbd>F5</kbd> | Insert Time & Date |
| <kbd>Ctrl</kbd> + <kbd>,</kbd> | Settings & Customization | <kbd>Ctrl</kbd> + <kbd>+</kbd> / <kbd>-</kbd> / <kbd>0</kbd> | Zoom In / Zoom Out / Reset 100% |

---

## 🤝 Contributing

Contributions are warmly welcomed! Please read our [CONTRIBUTING.md](CONTRIBUTING.md) guide for full guidelines.

1. **Fork** the repository and create your branch: `git checkout -b feature/your-feature`.
2. **Make your changes** following standard C# coding conventions and XAML theme brushes.
3. **Build & test** locally using `dotnet build -c Release`.
4. **Commit** with a clear message and open a **Pull Request**.

### Reporting Bugs & Feature Requests
Use [GitHub Issues](https://github.com/FerryPot/NotepadByFerryPot/issues) with our templates:
- [Bug Report](.github/ISSUE_TEMPLATE/bug_report.md): steps to reproduce, expected vs. actual behavior, Windows version, and screenshots.
- [Feature Request](.github/ISSUE_TEMPLATE/feature_request.md): describe problem, proposed solution, and UI ideas.

### Good First Issues
- **Tab Drag-Reordering**: Reorder open tabs via mouse drag.
- **Encoding Auto-Detection**: Detect BOM-less UTF-8, ANSI, and UTF-16 files automatically.
- **Zoom Persistence**: Remember user zoom setting across restarts.
- **Unit Tests**: Test suite for `MarkdownParserService` conversions.

---

## 🗺️ Roadmap

- [x] Multi-tab workspace with horizontal scroll overflow
- [x] Formatted and raw Markdown dual-mode editing
- [x] Formatting toolbar with active-state highlighting
- [x] Material Design 3 Light, Dark, and AMOLED themes
- [x] Modeless Find & Replace dialog
- [x] Real-time document statistics dialog
- [ ] Tab drag-and-drop reordering
- [ ] Character encoding auto-detection
- [ ] Code block syntax highlighting
- [ ] Custom keyboard shortcuts editor

---

## 📄 License & Credits

- **License**: Released under the [MIT License](LICENSE).
- **Third-Party Notices**: See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) for licenses and details of third-party open-source components.
- **Credits**: Built with [MaterialDesignInXamlToolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit) for Google Material Design 3 WPF controls.
