using Godot;
using Serilog;

namespace EnsembleRoot.Common.Utils;

/// <summary>Reads and writes persistent values in <see cref="UserDataCfgPath" />. Main thread only.</summary>
public static class UserData
{
	public static Variant GetValue(string section, string key, Variant fallback = default)
	{
		using var config = Load();
		return config?.GetValue(section, key, fallback) ?? fallback;
	}

	public static void SetValue(string section, string key, Variant value)
	{
		using var config = Load();

		if (config is null)
			return;

		config.SetValue(section, key, value);

		if (config.Save(UserDataCfgPath) is var result and not Error.Ok)
			Log.Warning("Failed to save {Path}: {Error}", UserDataCfgPath, result);
	}

	private static ConfigFile? Load()
	{
		var config = new ConfigFile();
		var result = config.Load(UserDataCfgPath);

		if (result is Error.Ok or Error.FileNotFound)
			return config;

		config.Dispose();
		Log.Warning("Failed to load {Path}: {Error}", UserDataCfgPath, result);

		return null;
	}
}
