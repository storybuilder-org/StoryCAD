# IOC → Constructor Injection Playbook (Issue #1063)

This is the exact, repeatable procedure to replace any `Ioc.Default.Get(Service|RequiredService)<T>()` calls with **constructor injection** across StoryCAD. Apply it **for each service in each batch**.

---

## Conventions
- **Fields**: `private readonly` + `_camelCase` (e.g., `_stateService`).
- **Prefer interfaces** when available (e.g., `ILogService`), else use the concrete.
- **Lifetimes**: **do not change** (remain *Singletons*). Services are registered in `ServiceLocator.cs`.

---

## Per‑Service Procedure

### 1) Find call sites
Search for both forms:
```
Ioc.Default.GetRequiredService<TheService>()
Ioc.Default.GetService<TheService>()
```

**IMPORTANT: Page classes (Views/*.xaml.cs) are OUT OF SCOPE**
- Pages that retrieve ViewModels via `Ioc.Default` should be skipped
- Only convert ViewModel and Service classes
- Page construction changes are tracked separately (per Issue #1063)

### 2) Verify registration
Ensure the service is registered in `ServiceLocator.cs` (singleton). If an interface exists, ensure interface→implementation registration.

### 3) Make the consumer DI‑friendly
Add a constructor parameter, store it in a `_field`, and remove the locator call(s).

**Example A — Single dependency**

*Before:*
```csharp
public class FooConsumer
{
    public void DoWork()
    {
        var state = Ioc.Default.GetRequiredService<AppState>(); // remove
        state.DoSomething();
    }
}
```

*After:*
```csharp
public class FooConsumer
{
    private readonly AppState _appState;

    public FooConsumer(AppState appState)
    {
        _appState = appState;
    }

    public void DoWork()
    {
        _appState.DoSomething();
    }
}
```

**Example B — Multiple dependencies (one optional)**

*Before:*
```csharp
public class BarConsumer
{
    public void Run()
    {
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>(); // remove
        var theme = Ioc.Default.GetService<ThemeService>();              // remove (may be null)
    }
}
```

*After:*
```csharp
public class BarConsumer
{
    private readonly PreferenceService _preferenceService;
    private readonly ThemeService? _themeService;

    public BarConsumer(PreferenceService preferenceService, ThemeService? themeService = null)
    {
        _preferenceService = preferenceService;
        _themeService = themeService;
    }

    public void Run()
    {
        if (_themeService != null) { /* ... */ }
    }
}
```

**Example C — Prefer interfaces (logging)**

*Before:*
```csharp
public class Baz
{
    public void Handle()
    {
        var log = Ioc.Default.GetRequiredService<LogService>(); // remove
        log.Log(LogLevel.Info, "Hello");
    }
}
```

*After:*
```csharp
using StoryCAD.Services.Logging;

public class Baz
{
    private readonly ILogService _log;

    public Baz(ILogService log) { _log = log; }

    public void Handle()
    {
        _log.Log(LogLevel.Info, "Hello");
    }
}
```

### 4) Replace all in‑class calls (worked example)
Remove every locator call, use injected fields.

*Before:*
```csharp
public class Qux
{
    public void Go()
    {
        var state = Ioc.Default.GetRequiredService<AppState>();
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        state.Tick();
        if (prefs.Model.AdvancedLogging) { /* ... */ }
    }
}
```

*After:*
```csharp
public class Qux
{
    private readonly AppState _appState;
    private readonly PreferenceService _preferenceService;

    public Qux(AppState appState, PreferenceService preferenceService)
    {
        _appState = appState;
        _preferenceService = preferenceService;
    }

    public void Go()
    {
        _appState.Tick();
        if (_preferenceService.Model.AdvancedLogging) { /* ... */ }
    }
}
```

### 5) Update construction sites
If the consumer was created with `new`, change the creation site to either:
- Resolve the consumer via DI **or**
- Pass required dependencies resolved from DI.

### 6) Special notes

**App.xaml.cs (many pulls)**
Convert `App` to accept its dependencies in the constructor and store as `_fields`. Keep behavior identical (startup, environment flags, logging).

**Logging (final batch)**
- Consumers must depend on **`ILogService`**.
- `LogService` must receive its own dependencies (e.g., `AppState`, `PreferenceService`) via its constructor and use fields internally. Behavior (file logs, console in debug, elmah) must be unchanged.

**SerializationLock (DI cleanup only)**
Inject `ILogService`, `AppState`, `PreferenceService`; remove any locator usage. Do **not** change `IsLocked` semantics here (tracked separately).

### 7) Tests & validation
- Update tests that `new` up classes (provide dependencies or build via DI).
- After each batch: run tests + perform a smoke run (launch, navigate, verify logs).
- **Grep gate** per service:
  ```bash
  git grep -n "Ioc.Default.GetRequiredService<TheService>"
  git grep -n "Ioc.Default.GetService<TheService>"
  ```
- Final: project‑wide grep shows **zero** `Ioc.Default`.

### 8) Commit template
```
DI: Replace Ioc.Default for <ServiceName> with constructor injection; keep singleton lifetime
```

---

## Batch‑by‑Batch Micro‑Plan (apply this for each batch)
1. Search and list all call sites for the batch’s service(s).
2. Edit each consumer: add ctor params, create `_fields`, replace all locator calls.
3. Fix construction sites (pass deps or resolve via DI).
4. Build + run tests + smoke test.
5. Grep gate for the batch’s service(s) → zero locator calls remain.
6. Commit with the template message.
7. Proceed to next batch.
