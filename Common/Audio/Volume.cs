using EnsembleRoot.Common.Utils;
using Godot;

namespace EnsembleRoot.Common.Audio;

/// <summary>The game's volume, as a linear gain on Godot's master bus, remembered across launches.</summary>
public static class Volume
{
	private const string Section = "audio";
	private const string MasterKey = "master_volume";
	private const int MasterBus = 0;

	/// <summary>From 0 (muted) to 1 (as authored).</summary>
	public static float Master
	{
		get => AudioServer.GetBusVolumeLinear(MasterBus);
		set
		{
			AudioServer.SetBusVolumeLinear(MasterBus, value);
			UserData.SetValue(Section, MasterKey, value);
		}
	}

	/// <summary>Applies the remembered volume; the user data file is hand-editable, so it's clamped.</summary>
	public static void Restore()
	{
		var volume = UserData.GetValue(Section, MasterKey, 1f).AsSingle();
		AudioServer.SetBusVolumeLinear(MasterBus, float.IsFinite(volume) ? Math.Clamp(volume, 0, 1) : 1);
	}
}
