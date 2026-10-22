# Contributing to Notepad by FerryPot

Thank you for your interest in contributing to **Notepad by FerryPot**! Contributions from the community help make this editor faster, more reliable, and more feature-rich.

---

## Code of Conduct

Please maintain a friendly, welcoming, and constructive environment. Treat everyone with respect and empathy.

---

## How to Report Bugs

When reporting an issue, please use the [Bug Report template](.github/ISSUE_TEMPLATE/bug_report.md) and include:
1. **Clear steps to reproduce** the issue.
2. **Expected behavior** vs. **actual behavior**.
3. **Environment details**: Windows version (10 or 11), .NET 8 runtime version, and active theme (Light, Dark, or AMOLED).
4. **Screenshots or error messages**, if applicable.

---

## How to Suggest Features

We welcome new feature proposals and enhancements! Please check existing issues first to avoid duplicates, then submit a [Feature Request](.github/ISSUE_TEMPLATE/feature_request.md) describing the use case and proposed design.

---

## Good First Issues

Looking for somewhere to start? Here are several high-value areas for contribution:
- **Tab Drag-and-Drop Reordering**: Allow rearranging open document tabs using mouse drag.
- **Encoding Auto-Detection**: Detect BOM-less UTF-8, ANSI, and UTF-16 files automatically upon opening.
- **Zoom Level Persistence**: Save and restore the user's zoom factor in `AppSettings`.
- **Unit Testing Suite**: Add unit tests for `MarkdownParserService` conversions (Markdown <-> FlowDocument).
- **Code Block Syntax Highlighting**: Add syntax highlighting for common programming languages inside code blocks.

---

## Development Workflow

### Prerequisites
- Windows 10 (version 1903+) or Windows 11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (with *.NET desktop development* workload) or VS Code with C# Dev Kit

### Steps

1. **Fork the repository** on GitHub.
2. **Clone your fork** locally:
   ```powershell
   git clone https://github.com/<your-username>/NotepadByFerryPot.git
   cd NotepadByFerryPot
   ```
3. **Create a topic branch**:
   ```powershell
   git checkout -b feature/your-feature-name
   ```
4. **Restore dependencies & build**:
   ```powershell
   dotnet restore
   dotnet build -c Debug
   ```
5. **Run the application**:
   ```powershell
   dotnet run
   ```
6. **Publish Release test**:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "Publish"
   ```
7. **Commit your changes**:
   Write clear, concise commit messages in the imperative mood (e.g., `Add zoom persistence across sessions`).
8. **Push to your fork and submit a Pull Request**:
   Describe the motivation behind your change and verify that existing functionality remains unaffected.

---

## Code Style & Guidelines

- **C# Conventions**: Follow standard Microsoft C# coding conventions and use nullable reference types where appropriate.
- **XAML & MVVM**: Keep styling in XAML resource dictionaries or control styles. Use Material Design 3 theme brushes (`MaterialDesignBody`, `MaterialDesign.Brush.Primary`, etc.) so controls adapt seamlessly across Light, Dark, and AMOLED modes.
- **Documentation**: Preserve existing code comments and docstrings.
- **Focused Scope**: Keep PRs focused on a single feature or bug fix.
