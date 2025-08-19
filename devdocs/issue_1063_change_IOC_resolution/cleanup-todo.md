# Post-Batch Cleanup TODO

## Unnecessary Variable Aliasing
After all DI conversion batches are complete, do a cleanup pass for unnecessary variable aliasing.

### Pattern to Find
Look for code like:
```csharp
var state = appState;  // or any injected field
// ... then using 'state' instead of 'appState' directly
```

### Example Found
In `OutlineViewModel.cs` line 1058:
```csharp
var state = appState;
//Only warns if it finds a node its referenced in
if (_foundElements.Count > 0 && !state.Headless)
```

Should just be:
```csharp
if (_foundElements.Count > 0 && !appState.Headless)
```

### Why This Happens
During DI conversion, we changed:
```csharp
var something = Ioc.Default.GetRequiredService<SomeService>();
```
To:
```csharp
var something = _someService;  // Unnecessary - just use _someService directly
```

### Search Strategy
After all batches complete, search for patterns like:
- `var [localVar] = _[field];`
- `var [localVar] = this.[property];`
- Look especially in methods that previously used Ioc.Default

This is harmless but makes code less clean and potentially confusing.

## Other Cleanup Items
(Add other post-batch cleanup items here as discovered)