namespace Maui.NativePopups;

public static class AppHostBuilderExtensions
{
	/// <summary>
	/// Adds native menus, action sheets, alerts, sheets, popovers, pickers and date/time pickers.
	/// </summary>
	public static MauiAppBuilder UseNativePopups(this MauiAppBuilder builder, Action<NativePopupsOptions>? configure = null)
	{
		var options = new NativePopupsOptions();
		configure?.Invoke(options);

		builder.Services.AddSingleton(options);

		return builder;
	}
}
