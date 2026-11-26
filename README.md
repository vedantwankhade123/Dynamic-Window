# Newton — A Premium Dynamic Newtonnd Experience for Windows

Native Windows 11 desktop productivity and live activity companion built with **C#**, **.NET 8**, **WinUI 3 (Windows App SDK)**, **MVVM**, **Dependency Injection**, and **SQLite**.

## Current Status: Phase 1 — Visual UI & Interaction Prototype

### Solution Structure
- `Newton.App` — WinUI 3 desktop application (`MainWindow`, `SettingsWindow`, `DemoWindow`, design system, controls, view models)
- `Newton.Core` — Domain models, service interfaces, `ActivityManager`, and `MockDemoService`
- `Newton.Infrastructure` — SQLite settings persistence (`SqliteSettingsService`), logging, DI registration
- `Newton.Windows` — Win32 / AppWindow floating newtonnd positioning, DPI scaling, rounded pill region helper
- `Newton.Tests` — xUnit tests for core activity management, demo simulation, and SQLite persistence

### Running Locally
```powershell
dotnet build Newton.sln -p:Platform=x64
dotnet test Newton.Tests\Newton.Tests.csproj
dotnet run --project Newton.App\Newton.App.csproj -p:Platform=x64
```
