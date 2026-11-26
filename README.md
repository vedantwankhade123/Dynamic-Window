# Isla — A Premium Dynamic Island Experience for Windows

Native Windows 11 desktop productivity and live activity companion built with **C#**, **.NET 8**, **WinUI 3 (Windows App SDK)**, **MVVM**, **Dependency Injection**, and **SQLite**.

## Current Status: Phase 1 — Visual UI & Interaction Prototype

### Solution Structure
- `Isla.App` — WinUI 3 desktop application (`MainWindow`, `SettingsWindow`, `DemoWindow`, design system, controls, view models)
- `Isla.Core` — Domain models, service interfaces, `ActivityManager`, and `MockDemoService`
- `Isla.Infrastructure` — SQLite settings persistence (`SqliteSettingsService`), logging, DI registration
- `Isla.Windows` — Win32 / AppWindow floating island positioning, DPI scaling, rounded pill region helper
- `Isla.Tests` — xUnit tests for core activity management, demo simulation, and SQLite persistence

### Running Locally
```powershell
dotnet build Isla.sln -p:Platform=x64
dotnet test Isla.Tests\Isla.Tests.csproj
dotnet run --project Isla.App\Isla.App.csproj -p:Platform=x64
```
