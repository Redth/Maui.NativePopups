# Maui.NativePopups

Native menus, action sheets, alerts, sheets, popovers, pickers and date/time pickers for .NET MAUI, using each platform's own controls:

| | iOS / Mac Catalyst | Android | Windows |
|---|---|---|---|
| Menus | `UIMenu` | `PopupMenu` | `MenuFlyout` |
| Action sheets | `UIAlertController`, anchored to the control that opened it | `PopupMenu` / Material 3 list dialog | `MenuFlyout` / `ContentDialog` |
| Alerts & prompts | `UIAlertController` | Material 3 dialog | `ContentDialog` |
| Sheets | `UISheetPresentationController` | Material 3 modal bottom sheet | `ContentDialog` |
| Popovers | `UIPopoverPresentationController` | anchored popup / bottom sheet | `Flyout` |
| Pickers | `UIMenu` or a searchable sheet | `PopupMenu` or a searchable bottom sheet | `MenuFlyout` / `ContentDialog` |
| Date / time | calendar / wheels popover | `MaterialDatePicker` / `MaterialTimePicker` | (WinUI's own) |

Supports .NET 10 and .NET 11.

> **Status:** early development — see [docs/ROADMAP.md](docs/ROADMAP.md). The implementation is being extracted from [Pool Math](https://www.troublefreepool.com/blog/poolmath/), where it ships today.

## Getting started

```csharp
builder
    .UseMauiApp<App>()
    .UseNativePopups(options =>
    {
        // Everything is on by default; for example:
        // options.ReplaceDateTimePickers = false;
        options.AndroidSeedColor = Color.FromArgb("#255AAB");
    });
```

## License

MIT
