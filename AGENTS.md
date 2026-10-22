# Project Rules for Notepad by FerryPot

## Mandatory Post-Change Workflow
For EVERY change made to this codebase:
1. **Publish Executable**: Always run `dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "D:\Notepad By FerryPot\Publish"` and copy the resulting `Publish\NotepadByFerryPot.exe` to the workspace root `D:\Notepad By FerryPot\NotepadByFerryPot.exe`.
2. **Git Commit**: Always stage and commit all modified files (including the updated `NotepadByFerryPot.exe`) to version control with a clear, descriptive commit message.
3. Keep the working tree clean at the end of each turn.
