# Roadmap

Maui.NativePopups packages the native presentation layer built for Pool Math (its PRs #489–#495) as a reusable plugin.

## Principles

- **Native first.** Each platform's own controls and animations (iOS 26 Liquid Glass morphs, Material 3, WinUI flyouts). We don't re-skin them to look alike.
- **Everything on by default**, each overridable in `NativePopupsOptions`.
- **Works from anywhere.** Use it from XAML attached properties, code-behind, or view models via `INativePopups`, with no platform references in view models.
- **net10 and net11** for Android, iOS, Mac Catalyst and Windows.
- **Trimming and NativeAOT safe.**

## API sketch

```csharp
public interface INativePopups
{
    Task<bool> AlertAsync(string title, string message, string accept, string? cancel = null);
    Task<string?> PromptAsync(string title, string message, PromptOptions? options = null);
    Task<string?> ActionSheetAsync(string? title, string? cancel, string? destruction, IEnumerable<string> buttons, PopupAnchor? anchor = null);
    Task<T?> PickAsync<T>(IEnumerable<T> items, PickerOptions<T> options, PopupAnchor? anchor = null);
    Task ShowSheetAsync(View content, SheetOptions? options = null);   // completes when dismissed
    Task ShowPopoverAsync(View content, PopupAnchor anchor, PopoverOptions? options = null);
    Task ShowMenuAsync(NativeMenu menu, PopupAnchor anchor);           // where the platform allows opening from code
}

// Anchors: a view, a rect within a view, a ToolbarItem, or the control the user last touched.
PopupAnchor.From(view); PopupAnchor.From(view, rect); PopupAnchor.From(toolbarItem); PopupAnchor.LastTouched;
```

XAML:

```xml
<Button Text="Add">
    <np:NativeMenu.Menu>
        <np:NativeMenu>
            <np:NativeMenuItem Text="Note" Icon="note.text" Command="{Binding AddNoteCommand}" />
            <np:NativeMenuItem Text="Delete" IsDestructive="True" Command="{Binding DeleteCommand}" />
        </np:NativeMenu>
    </np:NativeMenu.Menu>
</Button>

<ToolbarItem np:NativeMenu.Menu="{StaticResource ShareMenu}" ... />

<Label np:NativePicker.ItemsSource="{Binding Units}" np:NativePicker.SelectedItem="{Binding Unit}" />
```

Sheets for pages: `await Navigation.PushNativeSheetAsync(page, new SheetOptions { Size = SheetSize.Medium })`.

## Milestones

| # | Milestone | Notes |
|---|---|---|
| 0 | Repo, CI, sample app, packaging | ✅ this commit |
| 1 | Menus, action sheets, alerts, prompts, anchors, last-touch tracking, Android Material 3 theming from a seed color | from Pool Math #490/#491 |
| 2 | Sheets for views and pages (Fit / Medium / Large / custom, grabber, dismiss events, light/dark rebuild) | from #493 |
| 3 | Popovers (Mac Catalyst bar-button fix; Android popup-window or sheet) | from #489/#494 |
| 4 | Pickers (generic item model, menu vs. sheet rule, sections, search) | from #492 |
| 5 | Date / time picker replacement | from #495 |
| 6 | Windows: MenuFlyout, Flyout, ContentDialog for all of the above | new |
| 7 | Pool Math adopts the package | |

Each milestone ships a preview package and sample-app screenshots on iOS, Mac Catalyst, Android and Windows.

## Lessons carried over from Pool Math

- **Mac Catalyst:**
  - It never finishes presenting a popover whose source is a bar button item, so anchor to the item's view instead.
  - Catalyst popovers are separate windows and can overhang the app window.
- **Menus from code on iOS:** UIKit can't open a `UIMenu` programmatically. Attach it ahead of time (on our own button, or a clear overlay button), and fall back to a popover or sheet when opened from code.
- **Sizing fitted sheets and popovers:**
  - Re-measure after layout; the first measurement can come before native views exist.
  - Include safe-area and arrow insets.
  - MAUI 10+ layouts pad for safe areas by default, so hosted content needs `SafeAreaEdges="None"`.
- **Hosted MAUI content:**
  - Make it a logical child of the window so `DynamicResource`s resolve, and set its binding context explicitly.
  - Android: build it once and reuse it, because rebuilding churns Java peers and causes GC-bridge pauses mid-animation.
  - Android: rebuild a cached sheet after a light/dark switch, since the activity isn't recreated.
- **Material 3 on a Material 2 app theme:** layer the M3 attributes onto the activity theme without overriding existing ones (`Theme.ApplyStyle(…, force: false)`), so `MaterialDatePicker` / `MaterialTimePicker` get M3 typography and shapes.
- **Android toolbar buttons:** find them by touch position, because overlays defeat a deepest-view hit test.
- **UI tests:** keep accessibility ids on native rows and menu items (the item text and a sheet-title id) so Appium can drive them.
