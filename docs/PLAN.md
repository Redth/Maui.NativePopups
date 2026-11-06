# Maui.NativePopups: implementation plan

The working plan for building this library. `ROADMAP.md` is the short public version; this is the detailed one, written so a new session can pick up the work.

## 1. Goal and decisions

We're extracting the native popup layer built for **Pool Math** into a reusable, open-source .NET MAUI plugin. The layer covers menus, action sheets, alerts, sheets, popovers, pickers and date/time pickers.

Decisions made by the owner (Redth), 2026-11-06:

| Decision | Choice |
|---|---|
| Repo | Standalone public repo `Redth/Maui.NativePopups`. NuGet id `Redth.Maui.NativePopups`, namespace `Maui.NativePopups`. |
| Targets | **net10 and net11** for Android, iOS, Mac Catalyst and Windows. |
| Windows | **Real WinUI implementations in v1** (`MenuFlyout`, `Flyout`, `ContentDialog`), not just fallbacks. |
| Defaults | **Every feature on by default**, each overridable in `NativePopupsOptions`. This includes last-touched anchoring, the date/time picker replacement and the page dialog replacement. |
| Look | Use each platform's own controls and animations, e.g. the iOS 26 Liquid Glass morph out of a bar button and Material 3 on Android. Don't re-skin them to match each other. |
| CI | GitHub-hosted runners only (`windows-latest`, `xcode-27`). It's a public repo, so no self-hosted runners. |
| Pool Math | Its PRs #489–#495 are merged. Pool Math adopts this package at the end (milestone 7). |

## 2. Current state

**Milestone 0 is done:**
- the library project (all TFMs build)
- `UseNativePopups()` and `NativePopupsOptions`
- the sample app
- the slnx
- green CI
- README and ROADMAP

**There's no feature code yet.** `AppHostBuilderExtensions.UseNativePopups` only registers the options.

The repo layout:

```
src/Maui.NativePopups/
  Maui.NativePopups.csproj      net10.0/net11.0 x {portable, android, ios, maccatalyst} (+ windows when built on Windows)
  NativePopupsOptions.cs
  AppHostBuilderExtensions.cs
  Platforms/Apple/              compiled only for ios + maccatalyst
  Platforms/Android/            compiled only for android (Resources/ become AndroidResource)
  Platforms/Windows/            compiled only for windows
samples/NativePopups.Sample/    net11 MAUI app (template) referencing the library
docs/ROADMAP.md, docs/PLAN.md
.github/workflows/build.yml
```

## 3. Reference implementation in Pool Math

Pool Math is a private repo, `poolmath/PoolMath`, with a local checkout at `~/code/PoolMath`. All the code below is in **`main` @ `797efda6`**. It was written for one app, so generalize it as you port it. The PR numbers each have a detailed description with screenshots.

| Pool Math file (`src/maui/…`) | What it does | Plugin destination / notes |
|---|---|---|
| `Apple/AppleTouchTracker.cs` | Window-level gesture recognizer that records the last touched `UIView` without taking part in gestures | `Platforms/Apple/TouchTracker.cs`. Install per window from a lifecycle event (Pool Math does it in `TfpWindow.OnHandlerChanged`). |
| `Apple/AppleAnchors.cs` | Top presenter VC; `ToolbarItem` → `UIBarButtonItem` (by AutomationId, searching all VCs' navigation items); `FindView(accessibilityId)` | `Platforms/Apple/Anchors.cs`. Also resolve `ToolbarItem`s without an AutomationId: match by title/icon, or set a temporary id. |
| `Apple/AppleActionSheet.cs` | `UIAlertController` action sheet anchored to the last-touched view (falls back to centred with no arrow) | M1 `ActionSheetAsync` |
| `Apple/NativePopover.cs` | Hosts a MAUI `View` in a popover; `MauiContentController` with safe-area constraints; re-measures on layout and safe-area changes; Catalyst bar-button fix | M3 popovers, and the host that M2 sheets share |
| `Apple/PopoverAdaptiveDelegate.cs` | Keeps popovers as popovers on iPhone | shared |
| `Apple/AppleSheet.cs` | `UISheetPresentationController`; sizes Fit (custom detent, iOS 16+) / Medium / Large; grabber; user-dismiss callback | M2 |
| `Apple/ApplePicker.cs` | Sheet with an inset-grouped `UITableView` + `UISearchController`, sections, checkmark; `ApplePickerMenu` overlay `UIButton` + `UIMenu` with a deferred element | M4 |
| `Apple/AppleDateTimePickers.cs` | Mapper append: clear overlay button on MAUI date/time text fields → popover with an inline calendar or wheels | M5 |
| `Apple/AddLogMenu.cs` | App-specific `UIMenu` on a `UIButton` (`ShowsMenuAsPrimaryAction`), using `UIDeferredMenuElement` so items are rebuilt on open | Pattern for M1 `NativeMenu` (the contents stay in Pool Math) |
| `Platforms/Android/Native/NativeTheme.cs` | `ContextThemeWrapper` with `PmNativeTheme` | M1. Generate the theme from a seed colour (see §6). |
| `Platforms/Android/Native/AndroidTouchTracker.cs` | Records the touch point (fed from `MainActivity.DispatchTouchEvent`); finds a toolbar `ActionMenuView` child by position | M1. Needs a hook without subclassing the activity: wrap `Window.Callback`, or use lifecycle events. |
| `Platforms/Android/Native/AndroidDialogs.cs` | Material 3 alert; action sheet as a `PopupMenu` on the toolbar button or a list dialog; destructive item in the error colour | M1 |
| `Platforms/Android/Native/GlyphDrawable.cs` | Font glyph → `Drawable` for menu icons | M1 (menu icons from `FontImageSource`) |
| `Platforms/Android/Native/AddLogMenu.cs` | App `PopupMenu` with icons (`SetForceShowIcon`) | Pattern for M1 |
| `Platforms/Android/Native/NativeBottomSheet.cs` | Reusable `BottomSheetDialog` hosting MAUI content; rebuilt after a light/dark switch | M2 |
| `Platforms/Android/Native/AndroidSheet.cs` | One-shot bottom sheet for MAUI content, sized Fit/Medium/Large, with background tint | M2 |
| `Platforms/Android/Native/AndroidPicker.cs` | `PopupMenu` with a radio group, or a bottom sheet with an outlined search field, section headers and check rows | M4 |
| `Platforms/Android/Native/AndroidDateTimePickers.cs` | `MauiDatePicker` / `MauiTimePicker` `ShowPicker` → `MaterialDatePicker` / `MaterialTimePicker` | M5 |
| `Platforms/Android/Resources/values{,-night}/colors.xml`, `values/styles.xml` | Hand-written M3 tonal palette (`pm_m3_*`), `PmNativeTheme`, `PmDatePickerTheme`, `PmTimePickerTheme` | Replace with seed-generated roles (see §6). |
| `Platforms/Android/MainActivity.cs` | `Theme.ApplyStyle(PmNativeTheme, force: false)` adds the M3 attributes onto the M2 app theme; `DispatchTouchEvent` feeds the tracker | The plugin must do both without the app's activity (see §6). |
| `Controls/NativeSheets.cs` | `INativeSheetPage` + `NativeSheets.TryPresent/Dismiss`: detaches a page's card content and hosts it natively; window as logical parent | M2 `PushNativeSheetAsync(page)` / `ShowSheetAsync(view)` |
| `Controls/AnchoredContent.cs` | Popover on Apple, Fit bottom sheet on Android | M3 (Android choice: popup window or sheet) |
| `Controls/Entries/PickerRequest.cs` | Sections, search filter, the menu-or-sheet rule (12 items or fewer, flat, not searchable) | M4, generalized with `PickerOptions<T>` |
| `Controls/ToolbarItemBounds.cs` | Window-relative bounds of a toolbar item's native button (Android + Apple) | M3 popups on non-native fallbacks / Windows |
| `Controls/FlowingLayout.cs` | Not popup code, but it contains the fix to keep measure and arrange wrapping identically | Lesson only |
| `Services/HudServiceApp.cs` | Routes `DisplayAlert` / `DisplayActionSheet` to the native versions | Pattern for `ReplacePageDialogs` |
| `Services/NavigationServiceApp.cs` | `Popup<TVm>` → `NativeSheets.TryPresent`; `ClosePopup` → `NativeSheets.Dismiss` (also on user dismissal) | Pattern for page sheets |

## 4. Public API (design target)

```csharp
public interface INativePopups   // registered by UseNativePopups; also NativePopups.Current
{
    Task<bool> AlertAsync(string title, string message, string accept, string? cancel = null);
    Task<string?> PromptAsync(string title, string message, PromptOptions? options = null);
    Task<string?> ActionSheetAsync(string? title, string? cancel, string? destruction,
        IEnumerable<string> buttons, PopupAnchor? anchor = null);          // null anchor = LastTouched if enabled
    Task<T?> PickAsync<T>(IEnumerable<T> items, PickerOptions<T> options, PopupAnchor? anchor = null);
    Task ShowSheetAsync(View content, SheetOptions? options = null);      // completes when dismissed
    Task ShowPopoverAsync(View content, PopupAnchor anchor, PopoverOptions? options = null);
    Task<bool> ShowMenuAsync(NativeMenu menu, PopupAnchor anchor);        // false where the platform can't open from code
    void DismissSheet(View content);
    void DismissPopover(View content);
}

public sealed class PopupAnchor
{
    public static PopupAnchor From(VisualElement view, Rect? rectInView = null);
    public static PopupAnchor From(ToolbarItem item);
    public static PopupAnchor LastTouched { get; }
}

// Menus (XAML-friendly)
public class NativeMenu : BindableObject               // Items, Title
public class NativeMenuItem : BindableObject           // Text, Subtitle, Icon (ImageSource / SF Symbol name / drawable name),
                                                       // IsChecked, IsDestructive, IsEnabled, Command, CommandParameter
public class NativeMenuSection : NativeMenuItem        // Items (inline section with optional title)
public class NativeSubmenu : NativeMenuItem            // Items
public static class NativeMenuProperties               // attached: NativeMenu.Menu on View / Button / ToolbarItem

public sealed class SheetOptions   { SheetSize Size; bool ShowGrabber = true; Color? Background; bool IsDismissible = true; double? CustomHeight; }
public enum SheetSize { Fit, Medium, Large }
public sealed class PopoverOptions { double? MaxWidth; PopoverArrow PreferredArrow; AndroidPopoverStyle AndroidStyle /* PopupWindow | BottomSheet */; }
public sealed class PickerOptions<T>
{
    string? Title; Func<T,string> Display; Func<T,string?>? Description; Func<T,string?>? Section;
    T? Selected; bool Searchable; PickerPresentation Presentation /* Auto | Menu | Sheet */;
}

// Page sheets
public static class NavigationExtensions { Task PushNativeSheetAsync(this INavigation, Page page, SheetOptions? options = null); }

// Picker fields (XAML)
//   np:NativePicker.ItemsSource / SelectedItem / Title / Searchable / DisplayMemberPath on any View (opens on tap).
```

Defaults that act app-wide when `UseNativePopups()` is called (each can be switched off):
- `AnchorToLastTouchedControl`: install touch tracking per window.
- `ReplaceDateTimePickers`: append to the `DatePickerHandler` / `TimePickerHandler` mappers.
- `ReplacePageDialogs`: route `Page.DisplayAlert` / `DisplayActionSheet` / `DisplayPromptAsync` to the native versions. Check whether MAUI 10/11 exposes a public alert-manager hook first; otherwise document using `INativePopups` directly.

## 5. Platform mapping

| Feature | iOS / Mac Catalyst | Android | Windows |
|---|---|---|---|
| Menu on tap | `UIButton.Menu` + `ShowsMenuAsPrimaryAction`. On arbitrary views use a clear overlay `UIButton`; on `ToolbarItem` use `UIBarButtonItem.Menu`. Items come from a `UIDeferredMenuElement.CreateUncached`. | `PopupMenu` on click (`SetForceShowIcon`), group checkable | `MenuFlyout` (`FlyoutBase.SetAttachedFlyout` / `ShowAt`) |
| Menu from code | Not possible. Fall back to an anchored action sheet or popover list. | `PopupMenu.Show()` | `MenuFlyout.ShowAt` |
| Action sheet | `UIAlertController(.actionSheet)` with `PopoverPresentationController` source = anchor | `PopupMenu` on a toolbar button, else a Material list dialog | `MenuFlyout` at the anchor, else `ContentDialog` with buttons |
| Alert / prompt | `UIAlertController(.alert)` (+ text field) | `MaterialAlertDialogBuilder` (+ `TextInputLayout`) | `ContentDialog` (+ `TextBox`) |
| Sheet | `UISheetPresentationController` (detents, grabber) | `BottomSheetDialog` + `BottomSheetDragHandleView` | `ContentDialog`, or a full-height `Flyout`, hosting the MAUI view |
| Popover | `UIPopoverPresentationController` (`SourceItem` on iOS 16+; Catalyst uses the item's **view**) | `PopupWindow` (Material shape/elevation) anchored, or a sheet | `Flyout` with `Placement` |
| Picker | Menu (≤ N, flat, not searchable) or sheet with a table + search | `PopupMenu` (no descriptions) or a bottom sheet with search | `MenuFlyout` (`RadioMenuFlyoutItem`) or `ContentDialog` with `ListView` + `AutoSuggestBox` |
| Date/time | Overlay → popover with an inline calendar / wheels | `ShowPicker` → `MaterialDatePicker` / `MaterialTimePicker` | Leave WinUI's own pickers |

Hosting MAUI content natively uses `view.ToPlatform(mauiContext)`. On Windows that gives a `FrameworkElement` to place in a `Flyout` or `ContentDialog`.

## 6. Hard parts and lessons (read before coding)

1. **Mac Catalyst:**
   - A popover whose source is a `UIBarButtonItem` **never finishes presenting**. Anchor to the item's view instead, found by accessibility id.
   - Catalyst popovers are separate windows and may extend past the app window. That's normal, but window-cropped screenshots miss them.
2. **iOS can't open a `UIMenu` from code.** Attach it to a control ahead of time.
   - The overlay button must stay on top (`BringSubviewToFront` on refresh) and mirror `IsEnabled` and accessibility.
   - Use `UIDeferredMenuElement.CreateUncached` so items reflect the current state each time it opens.
3. **Sizing fitted sheets and popovers:**
   - The first measurement can run before the content's native views exist and be wrong. Re-measure in `ViewDidLayoutSubviews` and only apply a changed size.
   - Add safe-area insets (the arrow area) to `PreferredContentSize`.
   - A Fit sheet needs a custom detent (iOS 16+): content height + grabber room + bottom safe area.
4. **Safe areas in hosted content:** MAUI 10+ `Grid`s and `Layout`s default to `SafeAreaEdges=Container`. Hosted roots need `SafeAreaEdges.None`, or the content gets padded down, e.g. by the status bar while a card animates over it.
5. **Hosted MAUI content:**
   - Add it as a logical child of the **window**, or of the presenting page, so app-level `DynamicResource`s resolve. Set `BindingContext` explicitly. Remove the logical child on dismiss.
   - Detach from the old parent (`Border.Content`, `ContentView.Content`, `Layout.Remove`) before hosting.
6. **Android Java object churn:** on CoreCLR, creating and destroying native views causes GC-bridge pauses mid-animation. Build hosted content and dialogs once and reuse them. Don't create drawables per frame.
7. **Android light/dark:** the activity isn't recreated on a night-mode change (`ConfigChanges.UiMode`). Rebuild cached dialogs and sheets when `Configuration.UiMode & NightMask` changes, keeping the content view.
8. **Android theming on MAUI's Material 2 app theme:**
   - Wrap contexts with a Material 3 theme for the plugin's UI.
   - `MaterialDatePicker` / `MaterialTimePicker` are DialogFragments that use the **activity** theme plus an overlay. They need M3 typography and shape attributes the M2 theme lacks, so call `activity.Theme.ApplyStyle(<M3 theme>, force: false)`. That adds the missing attributes and overrides none, so the rest of the app is unchanged.
   - Do it in the plugin from `ActivityLifecycleCallbacks.OnActivityCreated`.
   - Map `colorTertiaryContainer` to the brand container, or the time picker's AM/PM toggle comes out pink.
   - **Seed colours:** Material Components can generate roles from a seed (`DynamicColorsOptions.Builder().setContentBasedSource(seedArgb)` + `DynamicColors.wrapContextIfAvailable`). Check the API exists in the bound version (`Xamarin.Google.Android.Material` 1.12+), and in the M2-theme case. If it doesn't, port the Material Color Utilities tonal palette (HCT) to C#.
9. **Android toolbar buttons:**
   - Find them by **touch position** among `ActionMenuView` children. A deepest-view hit test is defeated by overlays drawn above the toolbar.
   - Without a custom activity, get touches by wrapping `activity.Window.Callback` (`Window.ICallback.DispatchTouchEvent`) from lifecycle callbacks.
10. **Android `PopupMenu.Menu.Add`** has no `string` overload in the bindings; use `new Java.Lang.String(text)`. Set the content description to the item text for UI tests.
11. **Material dialogs:** `MaterialAlertDialogBuilder` must be given the M3-wrapped context. A destructive item gets an error-coloured `SpannableString`.
12. **Last-touched anchoring (both platforms):** only accept a touch from the last ~10 s, and only a view that's still attached. Otherwise fall back to centred with no arrow.
13. **Accessibility presses:** macOS automation (`osascript` System Events `click at`) does an AX press and doesn't produce a `UITouch`, so the touch tracker never sees it. Real clicks work. Synthetic `CGEvent` clicks were blocked in our environment.
14. **UI tests:**
    - Keep stable identifiers: menu items carry their text and content description, and sheet titles get an id (Pool Math used `labelPickerPopupTitle`).
    - Pool Math's Appium helper waits for "the title **or** the item".
15. **Android `DatePicker` hooks:** `MauiDatePicker.ShowPicker` / `MauiTimePicker.ShowPicker` are settable. Set them in a mapper append (custom key), which runs after `ConnectHandler`. They exist in MAUI 10.0.90+ and 11.
16. **iOS date/time:** don't replace the text field's delegate (MAUI owns it). Overlay a clear button instead.

## 7. Milestones and acceptance

Each milestone is **one PR**, with:
- sample-app screens for the feature
- screenshots on iOS (simulator), Mac Catalyst, Android (emulator) and Windows, attached as a release `screenshots-<PR#>` or in the PR
- green CI
- a preview NuGet package

### M1: Menus, action sheets, alerts, prompts, anchors, Android theming
- [ ] `PopupAnchor`; Apple and Android touch trackers installed via lifecycle events when `AnchorToLastTouchedControl` is on
- [ ] `NativeMenu` model + attached `NativeMenu.Menu` (`Button`, any `View`, `ToolbarItem`); icons from SF Symbol names / `FontImageSource` / images
- [ ] `ActionSheetAsync`, `AlertAsync`, `PromptAsync` (Apple, Android, Windows)
- [ ] Android M3 theme from a seed colour / dynamic colour; `ApplyStyle(force:false)` via lifecycle callbacks
- [ ] `ReplacePageDialogs`
- [ ] Sample: menus on a button, a view and a toolbar item; action sheet from a toolbar item; alert and prompt
- [ ] Unit tests for the portable parts (anchor resolution logic, option defaults)

### M2: Sheets
- [ ] `ShowSheetAsync(View, SheetOptions)`, `PushNativeSheetAsync(Page)`, dismiss and dismissed events, keyboard handling
- [ ] Android rebuild-on-night-mode; reuse; background colour; drag handle
- [ ] Windows `ContentDialog` (or `Flyout`) host
- [ ] Sample: a Fit sheet with a form, a Medium sheet with a long list, a Large page sheet

### M3: Popovers
- [ ] `ShowPopoverAsync`; the Catalyst bar-button fix; arrow/placement options; re-measure
- [ ] Android `PopupWindow` (Material surface, elevation, enter animation) or sheet per option
- [ ] Windows `Flyout`
- [ ] Sample: a filter-chips popover from a toolbar item; an editor popover pointing at a rect

### M4: Pickers
- [ ] `PickAsync<T>` with `PickerOptions<T>`; the Auto menu-or-sheet rule (`MaxMenuPickerItems`); sections; search; scroll to the selection
- [ ] `NativePicker.*` attached properties for tap-to-pick fields (the overlay menu on Apple)
- [ ] Windows `MenuFlyout` / `ContentDialog` + `AutoSuggestBox`
- [ ] Sample: short list, list with descriptions, grouped + searchable list of 200 items

### M5: Date/time
- [ ] Mapper appends behind `ReplaceDateTimePickers` (Apple overlay → popover; Android Material pickers with min/max)
- [ ] Respect MAUI's `DatePicker.MinimumDate` / `MaximumDate`, `Format`, and the 12/24h system setting
- [ ] Sample: log-entry style form

### M6: Windows completeness pass
- [ ] Parity check of all features on Windows; keyboard and accessibility; theme (light/dark)

### M7: Pool Math adopts the package
- [ ] Replace `src/maui/Apple/*`, `Platforms/Android/Native/*`, `NativeSheets`, `AnchoredContent`, `PickerRequest` (keep an `IDescribableItem` adapter), and the date/time mappings
- [ ] Keep the app-specific parts in Pool Math: the Add Log menu contents, the brand seed colour, `INativeSheetPage` page wiring
- [ ] Re-run Pool Math's validation flows (see the PR descriptions for #489–#495)

## 8. Dev environment and commands

- **SDK:** .NET 11 RC2 `11.0.100-rc.2.26504.105`, pinned with `rollForward: disable`. MAUI 11 `11.0.0-rc.2.26505.5`; MAUI 10 `10.0.120`. These are set in `Directory.Build.props`.
- **Build the library** (all non-Windows TFMs): `dotnet build src/Maui.NativePopups/Maui.NativePopups.csproj`.
- **Sample on the iOS simulator:** `dotnet build samples/NativePopups.Sample -f net11.0-ios -p:RuntimeIdentifier=iossimulator-arm64 -p:ValidateXcodeVersion=false`, then `xcrun simctl install <udid> …/NativePopups.Sample.app`.
- **Android:**
  - `dotnet build samples/NativePopups.Sample -f net11.0-android`.
  - Use a throwaway AVD from `~/android-sdk`, and delete it afterwards.
  - Don't let `ANDROID_HOME=~/android-sdk` leak into `dotnet build`: that SDK lacks API 37, so build with `env -u ANDROID_HOME -u ANDROID_SDK_ROOT`.
- **Mac Catalyst:** `dotnet build samples/NativePopups.Sample -f net11.0-maccatalyst -p:ValidateXcodeVersion=false`, then `open` the `.app`.
- **UI automation used during Pool Math development:**
  - iOS: `axe` (`axe tap --id/--label`, `axe describe-ui`, `xcrun simctl io <udid> screenshot`).
  - Android: `adb shell uiautomator dump` + `input tap`, and `adb emu screenrecord`. `adb shell screenrecord` only captured a few frames on the emulator.
- **CI recipe:**
  - Install workloads **without `--version`**: `maui` on macOS; `maui-windows wasm-tools-net10` on Windows.
  - Build each Windows TFM with a single-value `-p:TargetFrameworks=…`. `-f` still restores every TFM, and a `;` can't appear in `-p:`.
  - Apple sample builds need `-p:ValidateXcodeVersion=false`: the RC2 iOS SDK wants Xcode 27.0, and the runner has 27.1.
- **Trimming / AOT:** the library is `IsAotCompatible`. Avoid reflection, and keep NativeAOT warnings at zero.

## 9. Open questions to settle early

- `ReplacePageDialogs`: is there a public MAUI hook for the alert manager in net10/net11, or should we document `INativePopups` instead?
- Windows sheets: `ContentDialog`, or a custom bottom-anchored `Popup` for a sheet-like feel?
- Android popovers: does `PopupWindow` (anchored, Material surface) suit forms, or should the default be the bottom sheet (Pool Math's choice)?
- Menu icons: is a cross-platform `ImageSource`, plus SF Symbol / Android drawable names via `OnPlatform`, enough?
