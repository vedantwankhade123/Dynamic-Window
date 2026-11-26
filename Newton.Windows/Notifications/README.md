# Universal chat notification add-on

`UniversalReplyService` is an optional Windows-side service. It has no dependency on Newton views, the island window, or its view model. It exposes notification events and `SendReplyAsync`; a host adapter can translate `OnChatMessageReceived` into Newton's existing activity pipeline.

## Activation

Newton has a host adapter that maps received messages to its existing activity pipeline. Call its opt-in entry point from a user-initiated command (for example, a future notifications toggle):

```csharp
await App.EnableChatNotificationsAsync();
```

The add-on itself remains view-independent. The host adapter consumes its events; the service does not reference the island window or view model.

`CreateAsync` asks Windows for notification-listener access. Newton's package manifest now declares the user-notification-listener capability, but the app project still targets an unpackaged build. The opt-in entry point is ready, but it cannot succeed until Newton is installed with package identity (signed MSIX or sparse-package registration). Do not request notification access silently at startup.

## Reply support

Windows exposes notification text and app identity, but it does not expose a documented API to invoke another app's notification action or supply arbitrary `ValueSet` reply data. `SendReplyAsync` therefore routes only to explicitly supplied `IChatReplyProvider` implementations. With no provider it returns a clear unsupported result. Providers must use that app's supported API and credentials; they must not infer reply-action internals from toast payloads.

The generic toast binding is app-authored and varies. The service uses the first text element as the title and the remaining text as body, and reads the app logo when Windows supplies one. By default it filters to known chat/meeting app names; pass `isChatSource` to recognize additional installed apps. Windows cannot infer that a newly installed app is a chat client. This is best-effort parsing, not a guaranteed sender/conversation schema. Notifications are monitored only while the Newton process and service are running; durable background wake requires a packaged background-task design.
