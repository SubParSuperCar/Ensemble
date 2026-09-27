using Godot;
using Serilog;

namespace EnsembleRoot.Ui.Impl.ViewModels.Utils;

internal static class SessionPreferences
{
	private const string Section = "session";
	private const string DisplayNameKey = "display_name";

	public static string DisplayName
	{
		get => Load()?.GetValue(Section, DisplayNameKey, string.Empty).AsString() ?? string.Empty;
		set
		{
			if (Load() is not { } config)
				return;

			config.SetValue(Section, DisplayNameKey, value);

			if (config.Save(UserDataCfgPath) is var result and not Error.Ok)
				Log.Warning("Failed to save {Path}: {Error}", UserDataCfgPath, result);
		}
	}

	private static ConfigFile? Load()
	{
		var config = new ConfigFile();
		var result = config.Load(UserDataCfgPath);

		if (result is Error.Ok or Error.FileNotFound)
			return config;

		Log.Warning("Failed to load {Path}: {Error}", UserDataCfgPath, result);
		return null;
	}
}
