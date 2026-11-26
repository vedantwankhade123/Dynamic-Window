# DynamicWindow — A Premium Dynamic DynamicWindownd Experience for Windows

Native Windows 11 desktop productivity and live activity companion built with **C#**, **.NET 8**, **WinUI 3 (Windows App SDK)**, **MVVM**, **Dependency Injection**, and **SQLite**.

## Current Status: Phase 1 — Visual UI & Interaction Prototype

### Solution Structure
- `DynamicWindow.App` — WinUI 3 desktop application (`MainWindow`, `SettingsWindow`, `DemoWindow`, design system, controls, view models)
- `DynamicWindow.Core` — Domain models, service interfaces, `ActivityManager`, and `MockDemoService`
- `DynamicWindow.Infrastructure` — SQLite settings persistence (`SqliteSettingsService`), logging, DI registration
- `DynamicWindow.Windows` — Win32 / AppWindow floating dynamicwindownd positioning, DPI scaling, rounded pill region helper
- `DynamicWindow.Tests` — xUnit tests for core activity management, demo simulation, and SQLite persistence

### Running Locally
```powershell
dotnet build DynamicWindow.sln -p:Platform=x64
dotnet test DynamicWindow.Tests\DynamicWindow.Tests.csproj
dotnet run --project DynamicWindow.App\DynamicWindow.App.csproj -p:Platform=x64
```

## External activity integrations

DynamicWindow listens on the current user's named pipe `DynamicWindow.Island.Activity.v1`. Compatible background apps can connect and exchange UTF-8 JSON lines. Send a `hello` line first, then publish or update work with `upsert`, and finish it with `remove`:

```json
{"operation":"hello","source":"Build Agent"}
{"operation":"upsert","activityId":"job-42","source":"Build Agent","type":"CustomDeveloper","title":"Building project","subtitle":"Compiling 18 of 30 files","status":"Active","progress":60,"accentHex":"#0A84FF","actions":[{"id":"cancel","label":"Cancel"}]}
{"operation":"remove","activityId":"job-42"}
```

Supported activity types include `CustomDeveloper`, `AiGeneration`, `Download`, `FileOperation`, `AppInstall`, and `Notification`. Progress is a percentage from 0 to 100. The island sends an `action` JSON line back to the same client only after the user selects one of the activity's buttons. The pipe is restricted to the current Windows user. Apps must opt into this protocol; Windows does not provide universal discovery or live-task access for every installed app.
