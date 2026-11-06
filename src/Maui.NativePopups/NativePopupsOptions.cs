namespace Maui.NativePopups;

/// <summary>
/// Configures Maui.NativePopups. Every feature is on by default; turn off the ones you don't want.
/// </summary>
public sealed class NativePopupsOptions
{
	/// <summary>
	/// Action sheets shown without an explicit anchor attach to the control the user just touched
	/// (iOS 26 grows the sheet out of it; iPad / Mac / Android toolbar buttons show it on it).
	/// Requires tracking the last touch in each window.
	/// </summary>
	public bool AnchorToLastTouchedControl { get; set; } = true;

	/// <summary>
	/// Every MAUI <see cref="DatePicker"/> / <see cref="TimePicker"/> opens the platform's modern picker:
	/// a calendar / wheels popover on iOS and Mac Catalyst, Material 3 pickers on Android.
	/// </summary>
	public bool ReplaceDateTimePickers { get; set; } = true;

	/// <summary>
	/// Page.DisplayAlert / DisplayActionSheet use this library's native implementations
	/// (Material 3 dialogs on Android, anchored action sheets on Apple platforms).
	/// </summary>
	public bool ReplacePageDialogs { get; set; } = true;

	/// <summary>
	/// Android: the seed color the Material 3 color roles (primary, surfaces, ...) of this library's
	/// dialogs, sheets, menus and pickers are generated from. Null uses the app theme's colorPrimary.
	/// Only this library's UI is themed; the rest of the app keeps its theme.
	/// </summary>
	public Color? AndroidSeedColor { get; set; }

	/// <summary>Android 12+: use the wallpaper-based dynamic colors instead of <see cref="AndroidSeedColor"/>.</summary>
	public bool AndroidUseDynamicColors { get; set; }

	/// <summary>Lists with at most this many items (flat, not searchable) are shown as a menu on the field.</summary>
	public int MaxMenuPickerItems { get; set; } = 12;

	/// <summary>Default maximum width of popovers.</summary>
	public double PopoverMaxWidth { get; set; } = 400;
}
